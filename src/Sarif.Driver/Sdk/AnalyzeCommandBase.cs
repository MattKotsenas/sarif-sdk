﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.CodeAnalysis.Sarif.Driver
{
    public abstract class AnalyzeCommandBase<TContext, TOptions> : PlugInDriverCommand<TOptions>
        where TContext : IAnalysisContext, new()
        where TOptions : AnalyzeOptionsBase
    {
        public const string DefaultPolicyName = "default";

        // This is a unit test knob that configures the console output logger to capture its output
        // in a string builder. This is important to do because tooling behavior differs in non-trivial
        // ways depending on whether output is captured to a log file disk or not. In the latter case,
        // the captured output is useful to verify behavior.
        internal bool _captureConsoleOutput;
        internal ConsoleLogger _consoleLogger;

        private Tool _tool;
        private TContext _rootContext;
        private ResultsCachingLogger _resultsCachingLogger;
        private IDictionary<string, HashData> _pathToHashDataMap;

        public Exception ExecutionException { get; set; }

        public RuntimeConditions RuntimeErrors { get; set; }

        public static bool RaiseUnhandledExceptionInDriverCode { get; set; }

        public virtual FileFormat ConfigurationFormat { get { return FileFormat.Json; } }

        protected IFileSystem FileSystem { get; }

        protected AnalyzeCommandBase(IFileSystem fileSystem = null)
        {
            FileSystem = fileSystem ?? new FileSystem();
        }
        
        public string DefaultConfigurationPath
        {
            get
            {
                string currentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
                return Path.Combine(currentDirectory, "default.configuration.xml");
            }
        }

        public override int Run(TOptions analyzeOptions)
        {
            // 0. Initialize an common logger that drives all outputs. This
            //    object drives logging for console, statistics, etc.
            using (AggregatingLogger logger = InitializeLogger(analyzeOptions))
            {
                try
                {
                    Analyze(analyzeOptions, logger);
                }
                catch (ExitApplicationException<ExitReason> ex)
                {
                    // These exceptions have already been logged
                    ExecutionException = ex;
                    return FAILURE;
                }
                catch (Exception ex)
                {
                    // These exceptions escaped our net and must be logged here                    
                    RuntimeErrors |= Errors.LogUnhandledEngineException(_rootContext, ex);
                    ExecutionException = ex;
                    return FAILURE;
                }
                finally
                {
                    logger.AnalysisStopped(RuntimeErrors);
                }
            }

            bool succeeded = (RuntimeErrors & ~RuntimeConditions.Nonfatal) == RuntimeConditions.None;

            if (analyzeOptions.RichReturnCode)
            {
                return (int)RuntimeErrors;
            }

            return succeeded ? SUCCESS : FAILURE;
        }

        private void Analyze(TOptions analyzeOptions, AggregatingLogger logger)
        {
            // 0. Log analysis initiation
            logger.AnalysisStarted();

            // 1. Create context object to pass to skimmers. The logger
            //    and configuration objects are common to all context
            //    instances and will be passed on again for analysis.
            _rootContext = CreateContext(analyzeOptions, logger, RuntimeErrors);

            // 2. Perform any command line argument validation beyond what
            //    the command line parser library is capable of.
            ValidateOptions(_rootContext, analyzeOptions);

            // 3. Produce a comprehensive set of analysis targets 
            HashSet<string> targets = CreateTargetsSet(analyzeOptions);

            // 4. Proactively validate that we can locate and 
            //    access all analysis targets. Helper will return
            //    a list that potentially filters out files which
            //    did not exist, could not be accessed, etc.
            targets = ValidateTargetsExist(_rootContext, targets);

            // 5. Initialize report file, if configured.
            InitializeOutputFile(analyzeOptions, _rootContext, targets);

            // 6. Instantiate skimmers.
            HashSet<Skimmer<TContext>> skimmers = CreateSkimmers(_rootContext);

            // 7. Initialize configuration. This step must be done after initializing
            //    the skimmers, as rules define their specific context objects and
            //    so those assemblies must be loaded.
            InitializeConfiguration(analyzeOptions, _rootContext);

            // 8. Initialize skimmers. Initialize occurs a single time only. This
            //    step needs to occurs after initializing configuration in order
            //    to allow command-line override of rule settings
            skimmers = InitializeSkimmers(skimmers, _rootContext);

            // 9. Run all analysis
            AnalyzeTargets(analyzeOptions, skimmers, _rootContext, targets);

            // 10. For test purposes, raise an unhandled exception if indicated
            if (RaiseUnhandledExceptionInDriverCode)
            {
                throw new InvalidOperationException(GetType().Name);
            }
        }

        protected virtual void ValidateOptions(TContext context, TOptions analyzeOptions)
        {
            bool succeeded = true;

            succeeded &= ValidateFile(context, analyzeOptions.OutputFilePath, shouldExist: null);
            succeeded &= ValidateFile(context, analyzeOptions.ConfigurationFilePath, shouldExist: true);
            succeeded &= ValidateFiles(context, analyzeOptions.PluginFilePaths, shouldExist: true);
            succeeded &= ValidateInvocationPropertiesToLog(context, analyzeOptions.InvocationPropertiesToLog);
            succeeded &= ValidateOutputFileCanBeCreated(context, analyzeOptions.OutputFilePath, analyzeOptions.Force);

            if (!succeeded)
            {
                ThrowExitApplicationException(context, ExitReason.InvalidCommandLineOption);
            }
        }

        private bool ValidateFiles(TContext context, IEnumerable<string> filePaths, bool shouldExist)
        {
            if (filePaths == null) { return true; }

            bool succeeded = true;

            foreach (string filePath in filePaths)
            {
                succeeded &= ValidateFile(context, filePath, shouldExist);
            }

            return succeeded;
        }

        private bool ValidateFile(TContext context, string filePath, bool? shouldExist)
        {
            if (filePath == null || filePath == DefaultPolicyName) { return true; }

            Exception exception = null;

            try
            {
                bool fileExists = FileSystem.FileExists(filePath);

                if (fileExists || shouldExist == null || !shouldExist.Value)
                {
                    return true;
                }

                Errors.LogMissingFile(context, filePath);
            }
            catch (IOException ex) { exception = ex; }
            catch (SecurityException ex) { exception = ex; }
            catch (UnauthorizedAccessException ex) { exception = ex; }

            if (exception != null)
            {
                Errors.LogExceptionAccessingFile(context, filePath, exception);
            }

            return false;
        }

        private static bool ValidateInvocationPropertiesToLog(TContext context, IEnumerable<string> propertiesToLog)
        {
            bool succeeded = true;

            if (propertiesToLog != null)
            {
                List<string> validPropertyNames = typeof(Invocation).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(propInfo => propInfo.Name.ToUpperInvariant())
                    .ToList();

                foreach (string propertyName in propertiesToLog)
                {
                    if (!validPropertyNames.Contains(propertyName.ToUpperInvariant()))
                    {
                        Errors.LogInvalidInvocationPropertyName(context, propertyName);
                        succeeded = false;
                    }
                }
            }

            return succeeded;
        }

        private bool ValidateOutputFileCanBeCreated(TContext context, string outputFilePath, bool force)
        {
            bool succeeded = true;

            if (!DriverUtilities.CanCreateOutputFile(outputFilePath, force, FileSystem))
            {
                Errors.LogOutputFileAlreadyExists(context, outputFilePath);
                succeeded = false;
            }

            return succeeded;
        }

        internal AggregatingLogger InitializeLogger(AnalyzeOptionsBase analyzeOptions)
        {
            _tool = Tool.CreateFromAssemblyData();

            var logger = new AggregatingLogger();

            if (!analyzeOptions.Quiet)
            {
                _consoleLogger = new ConsoleLogger(analyzeOptions.Verbose, _tool.Driver.Name) { CaptureOutput = _captureConsoleOutput };
                logger.Loggers.Add(_consoleLogger);
            }

            if (analyzeOptions.ComputeFileHashes)
            {
                _resultsCachingLogger = new ResultsCachingLogger(analyzeOptions.Verbose);
                logger.Loggers.Add(_resultsCachingLogger);
            }

            if (analyzeOptions.Statistics)
            {
                logger.Loggers.Add(new StatisticsLogger());
            }

            return logger;
        }

        private HashSet<string> CreateTargetsSet(TOptions analyzeOptions)
        {
            HashSet<string> targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string specifier in analyzeOptions.TargetFileSpecifiers)
            {
                string normalizedSpecifier = specifier;

                Uri uri;
                if (Uri.TryCreate(specifier, UriKind.RelativeOrAbsolute, out uri))
                {
                    if (uri.IsAbsoluteUri && (uri.IsFile || uri.IsUnc))
                    {
                        normalizedSpecifier = uri.LocalPath;
                    }
                }
                // Currently, we do not filter on any extensions.
                var fileSpecifier = new FileSpecifier(normalizedSpecifier, recurse: analyzeOptions.Recurse, fileSystem: FileSystem);
                foreach (string file in fileSpecifier.Files) { targets.Add(file); }
            }
           return targets;
        }

        private HashSet<string> ValidateTargetsExist(TContext context, HashSet<string> targets)
        {
            if (targets.Count == 0)
            {
                Errors.LogNoValidAnalysisTargets(context);
                ThrowExitApplicationException(context, ExitReason.NoValidAnalysisTargets);
            }

            return targets;
        }

        protected virtual TContext CreateContext(
            TOptions options,
            IAnalysisLogger logger,
            RuntimeConditions runtimeErrors,
            string filePath = null)
        {
            var context = new TContext();
            context.Logger = logger;
            context.RuntimeErrors = runtimeErrors;

            if (filePath != null)
            {
                context.TargetUri = new Uri(filePath);

                if (options.ComputeFileHashes)
                {
                    if (_pathToHashDataMap != null && _pathToHashDataMap.TryGetValue(filePath, out HashData hashData))
                    {
                        context.Hashes = hashData;
                    }
                    else
                    {
                        context.Hashes = HashUtilities.ComputeHashes(filePath);
                    }
                }
            }

            return context;
        }

        /// <summary>
        /// Calculate the file to load the configuration from.
        /// </summary>
        /// <param name="options">Options</param>
        /// <param name="unitTestFileExists">Used only in unit testing, overrides "File.Exists".  
        /// TODO--Restructure Sarif.Driver to use Sarif.IFileSystem for actions on file, to enable unit testing here instead.</param>
        /// <returns>Configuration file path, or null if the built in configuration should be used.</returns>
        internal string GetConfigurationFileName(TOptions options, bool unitTestFileExists = false)
        {
            if (options.ConfigurationFilePath == DefaultPolicyName)
            {
                return null;
            }

            if (String.IsNullOrEmpty(options.ConfigurationFilePath))
            {
                if (!FileSystem.FileExists(DefaultConfigurationPath) && !unitTestFileExists)
                {
                    return null;
                }

                return DefaultConfigurationPath;
            }
            return options.ConfigurationFilePath;
        }
        
        protected virtual void InitializeConfiguration(TOptions options, TContext context)
        {
            context.Policy = new PropertiesDictionary();

            string configurationFileName = GetConfigurationFileName(options);
            if (string.IsNullOrEmpty(configurationFileName))
            {
                return;
            }

            string extension = Path.GetExtension(configurationFileName);

            if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                context.Policy.LoadFromXml(configurationFileName);
            }
            else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                context.Policy.LoadFromJson(configurationFileName);
            }
            else if (ConfigurationFormat == FileFormat.Xml)
            {
                context.Policy.LoadFromXml(configurationFileName);
            }
            else
            {
                context.Policy.LoadFromJson(configurationFileName);
            }
        }

        private void InitializeOutputFile(TOptions analyzeOptions, TContext context, HashSet<string> targets)
        {
            string filePath = analyzeOptions.OutputFilePath;
            AggregatingLogger aggregatingLogger = (AggregatingLogger)context.Logger;

            if (!string.IsNullOrEmpty(filePath))
            {
                InvokeCatchingRelevantIOExceptions
                (
                    () =>
                    {
                        LoggingOptions loggingOptions;
                        loggingOptions = analyzeOptions.ConvertToLoggingOptions();

                        OptionallyEmittedData dataToInsert = analyzeOptions.DataToInsert.ToFlags();
                        OptionallyEmittedData dataToRemove = analyzeOptions.DataToRemove.ToFlags();

                        // This code is required in order to support the obsolete ComputeFileHashes argument
                        // on the analyze command-line.
                        if (analyzeOptions.ComputeFileHashes) { dataToInsert |= OptionallyEmittedData.Hashes; }

                        SarifLogger sarifLogger;

                        if (analyzeOptions.SarifOutputVersion != SarifVersion.OneZeroZero)
                        {
                            sarifLogger = new SarifLogger(
                                    analyzeOptions.OutputFilePath,
                                    loggingOptions,
                                    dataToInsert,
                                    dataToRemove,
                                    tool: _tool,
                                    run: null,
                                    analysisTargets: targets,
                                    invocationTokensToRedact: GenerateSensitiveTokensList(),
                                    invocationPropertiesToLog: analyzeOptions.InvocationPropertiesToLog);
                        }
                        else
                        {
                            sarifLogger = new SarifOneZeroZeroLogger(
                                    analyzeOptions.OutputFilePath,
                                    loggingOptions,
                                    dataToInsert,
                                    dataToRemove,
                                    tool: _tool,
                                    run: null,
                                    analysisTargets: targets,
                                    invocationTokensToRedact: GenerateSensitiveTokensList(),
                                    invocationPropertiesToLog: analyzeOptions.InvocationPropertiesToLog);
                        }
                        _pathToHashDataMap = sarifLogger.AnalysisTargetToHashDataMap;
                        sarifLogger.AnalysisStarted();
                        aggregatingLogger.Loggers.Add(sarifLogger);
                    },
                    (ex) =>
                    {
                        Errors.LogExceptionCreatingLogFile(context, filePath, ex);
                        ThrowExitApplicationException(context, ExitReason.ExceptionCreatingLogFile, ex);
                    }
                );
            }
        }

        private IEnumerable<string> GenerateSensitiveTokensList()
        {
            var result = new List<String>();

            result.Add(Environment.MachineName);
            result.Add(Environment.UserName);
            result.Add(Environment.UserDomainName);

            string userDnsDomain = Environment.GetEnvironmentVariable("USERDNSDOMAIN");
            string logonServer = Environment.GetEnvironmentVariable("LOGONSERVER");

            if (!string.IsNullOrEmpty(userDnsDomain)) { result.Add(userDnsDomain); }
            if (!string.IsNullOrEmpty(logonServer)) { result.Add(logonServer); }

            return result;
        }

        public void InvokeCatchingRelevantIOExceptions(Action action, Action<Exception> exceptionHandler)
        {
            try
            {
                action();
            }
            catch (UnauthorizedAccessException ex)
            {
                exceptionHandler(ex);
            }
            catch (IOException ex)
            {
                exceptionHandler(ex);
            }
        }

        private HashSet<Skimmer<TContext>> CreateSkimmers(TContext context)
        {
            IEnumerable<Skimmer<TContext>> skimmers;
            HashSet<Skimmer<TContext>> result = new HashSet<Skimmer<TContext>>();

            try
            {
                skimmers = CompositionUtilities.GetExports<Skimmer<TContext>>(DefaultPlugInAssemblies);

                SupportedPlatform currentOS = GetCurrentRunningOS();
                foreach (Skimmer<TContext> skimmer in skimmers)
                {
                    if(skimmer.SupportedPlatforms.HasFlag(currentOS))
                    {
                        result.Add(skimmer);
                    }
                    else
                    {
                        Warnings.LogUnsupportedPlatformForRule(context, skimmer.Name, skimmer.SupportedPlatforms, currentOS);
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.LogExceptionInstantiatingSkimmers(context, DefaultPlugInAssemblies, ex);
                ThrowExitApplicationException(context, ExitReason.UnhandledExceptionInstantiatingSkimmers, ex);
            }

            if (result.Count == 0)
            {
                Errors.LogNoRulesLoaded(context);
                ThrowExitApplicationException(context, ExitReason.NoRulesLoaded);
            }
            return result;
        }

        private SupportedPlatform GetCurrentRunningOS()
        {
            // RuntimeInformation is not present in NET452.
#if NET452
            return SupportedPlatform.Windows;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return SupportedPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return SupportedPlatform.OSX;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return SupportedPlatform.Windows;
            }
            else
            {
                return SupportedPlatform.Unknown;
            }
#endif
        }

        protected virtual void AnalyzeTargets(
            TOptions options,
            IEnumerable<Skimmer<TContext>> skimmers,
            TContext rootContext,
            IEnumerable<string> targets)
        {
            HashSet<string> disabledSkimmers = new HashSet<string>();

            foreach (Skimmer<TContext> skimmer in skimmers)
            {
                PerLanguageOption<RuleEnabledState> ruleEnabledProperty;
                ruleEnabledProperty = DefaultDriverOptions.CreateRuleSpecificOption(skimmer, DefaultDriverOptions.RuleEnabled);

                RuleEnabledState ruleEnabled = rootContext.Policy.GetProperty(ruleEnabledProperty);

                if (ruleEnabled == RuleEnabledState.Disabled)
                {
                    disabledSkimmers.Add(skimmer.Id);
                    Warnings.LogRuleExplicitlyDisabled(rootContext, skimmer.Id);
                    RuntimeErrors |= RuntimeConditions.RuleWasExplicitlyDisabled;
                }
            }

            if (disabledSkimmers.Count == skimmers.Count())
            {
                Errors.LogAllRulesExplicitlyDisabled(rootContext);
                ThrowExitApplicationException(rootContext, ExitReason.NoRulesLoaded);
            }

            if (options.ComputeFileHashes)
            {
                // If analysis is persisted to a disk log file, we will have already
                // computed all file hashes and stored them to _pathToHashDataMap.
                _pathToHashDataMap = _pathToHashDataMap ?? HashUtilities.MultithreadedComputeTargetFileHashes(targets);
            }

            foreach (string target in targets)
            {
                using (TContext context = DetermineApplicabilityAndAnalyze(options, skimmers, rootContext, target, disabledSkimmers))
                {
                    RuntimeErrors |= context.RuntimeErrors;
                }
            }
        }

        protected virtual TContext DetermineApplicabilityAndAnalyze(
            TOptions options,
            IEnumerable<Skimmer<TContext>> skimmers,
            TContext rootContext,
            string target,
            HashSet<string> disabledSkimmers)
        {
            var context = CreateContext(options, rootContext.Logger, rootContext.RuntimeErrors, target);
            context.Policy = rootContext.Policy;

            if (options.Verbose)
            {
                Console.WriteLine(string.Format(CultureInfo.CurrentCulture,
                    @"Analyzing '{0}'..",
                    context.TargetUri.GetFileName()));
            }

            if (options.ComputeFileHashes)
            {
                _resultsCachingLogger.HashToResultsMap.TryGetValue(context.Hashes.Sha256, out List<Tuple<ReportingDescriptor, Result>> cachedResults);
                _resultsCachingLogger.HashToNotificationsMap.TryGetValue(context.Hashes.Sha256, out List<Notification> cachedNotifications);

                bool replayCachedData = (cachedResults != null || cachedNotifications != null);

                if (replayCachedData)
                {
                    context.Logger.AnalyzingTarget(context);

                    if (cachedResults != null)
                    {
                        foreach (Tuple<ReportingDescriptor, Result> cachedResult in cachedResults)
                        {
                            if (cachedResult.Item2.Locations?.Count > 0)
                            {
                                cachedResult.Item2.Locations[0].PhysicalLocation.ArtifactLocation.Uri = context.TargetUri;
                            }
                            context.Logger.Log(cachedResult.Item1, cachedResult.Item2);
                        }
                    }

                    if (cachedNotifications != null)
                    {
                        foreach (Notification notification in cachedNotifications)
                        {
                            context.Logger.LogConfigurationNotification(notification);
                        }
                    }
                    return context;
                }
            }

            if (context.TargetLoadException != null)
            {
                Errors.LogExceptionLoadingTarget(context);
                context.Dispose();
                return context;
            }
            else if (!context.IsValidAnalysisTarget)
            {
                Warnings.LogExceptionInvalidTarget(context);
                context.Dispose();
                return context;
            }

            context.Logger.AnalyzingTarget(context);
            IEnumerable<Skimmer<TContext>> applicableSkimmers = DetermineApplicabilityForTarget(skimmers, context, disabledSkimmers);
            AnalyzeTarget(applicableSkimmers, context, disabledSkimmers);

            return context;
        }

        protected virtual void AnalyzeTarget(IEnumerable<Skimmer<TContext>> skimmers, TContext context, HashSet<string> disabledSkimmers)
        {
            foreach (Skimmer<TContext> skimmer in skimmers)
            {
                if (disabledSkimmers.Contains(skimmer.Id)) { continue; }

                context.Rule = skimmer;

                try
                {
                    skimmer.Analyze(context);
                }
                catch (Exception ex)
                {
                    RuntimeErrors |= Errors.LogUnhandledRuleExceptionAnalyzingTarget(disabledSkimmers, context, ex);
                }
            }
        }

        protected virtual IEnumerable<Skimmer<TContext>> DetermineApplicabilityForTarget(
            IEnumerable<Skimmer<TContext>> skimmers,
            TContext context,
            HashSet<string> disabledSkimmers)
        {
            var candidateSkimmers = new List<Skimmer<TContext>>();

            foreach (Skimmer<TContext> skimmer in skimmers)
            {
                if (disabledSkimmers.Contains(skimmer.Id)) { continue; }

                string reasonForNotAnalyzing = null;
                context.Rule = skimmer;

                AnalysisApplicability applicability = AnalysisApplicability.Unknown;

                try
                {
                    applicability = skimmer.CanAnalyze(context, out reasonForNotAnalyzing);
                }
                catch (Exception ex)
                {
                    Errors.LogUnhandledRuleExceptionAssessingTargetApplicability(disabledSkimmers, context, ex);
                    continue;
                }
                finally
                {
                    RuntimeErrors |= context.RuntimeErrors;
                }

                switch (applicability)
                {
                    case AnalysisApplicability.NotApplicableToSpecifiedTarget:
                        {
                            Notes.LogNotApplicableToSpecifiedTarget(context, reasonForNotAnalyzing);
                            break;
                        }

                    case AnalysisApplicability.ApplicableToSpecifiedTarget:
                        {
                            candidateSkimmers.Add(skimmer);
                            break;
                        }
                }
            }
            return candidateSkimmers;
        }

        protected void ThrowExitApplicationException(TContext context, ExitReason exitReason, Exception innerException = null)
        {
            RuntimeErrors |= context.RuntimeErrors;

            throw new ExitApplicationException<ExitReason>(DriverResources.MSG_UnexpectedApplicationExit, innerException)
            {
                ExitReason = exitReason
            };
        }

        protected virtual HashSet<Skimmer<TContext>> InitializeSkimmers(HashSet<Skimmer<TContext>> skimmers, TContext context)
        {
            HashSet<Skimmer<TContext>> disabledSkimmers = new HashSet<Skimmer<TContext>>();

            // ONE-TIME initialization of skimmers. Do not call 
            // Initialize more than once per skimmer instantiation
            foreach (Skimmer<TContext> skimmer in skimmers)
            {
                try
                {
                    context.Rule = skimmer;
                    skimmer.Initialize(context);
                }
                catch (Exception ex)
                {
                    RuntimeErrors |= RuntimeConditions.ExceptionInSkimmerInitialize;
                    Errors.LogUnhandledExceptionInitializingRule(context, ex);
                    disabledSkimmers.Add(skimmer);
                }
            }

            foreach (Skimmer<TContext> disabledSkimmer in disabledSkimmers)
            {
                skimmers.Remove(disabledSkimmer);
            }

            return skimmers;
        }

        protected static void LogToolNotification(
            IAnalysisLogger logger,
            string message,
            FailureLevel level = FailureLevel.Note,
            Exception ex = null)
        {
            ExceptionData exceptionData = null;
            if (ex != null)
            {
                exceptionData = new ExceptionData
                {
                    Kind = ex.GetType().FullName,
                    Message = ex.Message,
                    Stack = Stack.CreateStacks(ex).FirstOrDefault()
                };
            }

            TextWriter writer = level == FailureLevel.Error ? Console.Error : Console.Out;
            writer.WriteLine(message);

            logger.LogToolNotification(new Notification
            {
                Level = level,
                Message = new Message { Text = message },
                Exception = exceptionData
            });
        }
    }
}