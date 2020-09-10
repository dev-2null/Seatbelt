﻿using System;
using Microsoft.Win32;
using System.Collections.Generic;
using Seatbelt.Output.TextWriters;
using Seatbelt.Output.Formatters;
using Seatbelt.Util;


namespace Seatbelt.Commands.Windows
{
    internal class WindowsDefenderCommand : CommandBase
    {
        public override string Command => "WindowsDefender";
        public override string Description => "Windows Defender settings (including exclusion locations)";
        public override CommandGroup[] Group => new[] { CommandGroup.System, CommandGroup.Remote };
        public override bool SupportRemote => true;
        public Runtime ThisRunTime;

        public WindowsDefenderCommand(Runtime runtime) : base(runtime)
        {
            ThisRunTime = runtime;
        }

        public override IEnumerable<CommandDTOBase?> Execute(string[] args)
        {
            var pathExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Paths");
            var pathExclusions = new List<string>();
            foreach (var kvp in pathExclusionData)
            {
                pathExclusions.Add(kvp.Key);
            }

            List<string> excludedPathsList = new List<string>();
            var excludedPaths = ThisRunTime.GetStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Windows Defender\Policy Manager", "ExcludedPaths");
            if (excludedPaths != null)
            {
                foreach (var s in excludedPaths.Split('|'))
                {
                    excludedPathsList.Add(s);
                }
            }

            var processExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Processes");
            var processExclusions = new List<string>();
            foreach (var kvp in processExclusionData)
            {
                processExclusions.Add(kvp.Key);
            }

            var extensionExclusionData = ThisRunTime.GetValues(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Exclusions\Extensions");
            var extensionExclusions = new List<string>();
            foreach (var kvp in extensionExclusionData)
            {
                extensionExclusions.Add(kvp.Key);
            }

            var asrKeyPath = @"Software\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR";
            var asrEnabled = RegistryUtil.GetDwordValue(RegistryHive.LocalMachine, asrKeyPath, "ExploitGuard_ASR_Rules");

            var asrSettings = new AsrSettings(
                asrEnabled != null && (asrEnabled != 0)
                );

            foreach (var value in RegistryUtil.GetValues(RegistryHive.LocalMachine, $"{asrKeyPath}\\Rules"))
            {
                asrSettings.Rules.Add(new AsrRule(
                    new Guid(value.Key),
                    int.Parse((string)value.Value)
                ));
            }

            foreach (var value in RegistryUtil.GetValues(RegistryHive.LocalMachine, $"{asrKeyPath}\\ASROnlyExclusions"))
            {
                asrSettings.Exclusions.Add(value.Key);
            }

            yield return new WindowsDefenderDTO(
                pathExclusions,
                excludedPathsList,
                processExclusions,
                extensionExclusions,
                asrSettings
            );
        }
    }

    internal class AsrRule
    {
        public AsrRule(Guid rule, int state)
        {
            Rule = rule;
            State = state;
        }
        public Guid Rule { get; }
        public int State { get; }
    }
    internal class AsrSettings
    {
        public AsrSettings(bool enabled)
        {
            Enabled = enabled;
            Rules = new List<AsrRule>();
            Exclusions = new List<string>();
        }

        public bool Enabled { get; }
        public List<AsrRule> Rules { get; }
        public List<string> Exclusions { get; }
    }

    internal class WindowsDefenderDTO : CommandDTOBase
    {
        public WindowsDefenderDTO(List<string> pathExclusions, List<string> policyManagerPathExclusions, List<string> processExclusions, List<string> extensionExclusions, AsrSettings asrSettings)
        {
            PathExclusions = pathExclusions;
            PolicyManagerPathExclusions = policyManagerPathExclusions;
            ProcessExclusions = processExclusions;
            ExtensionExclusions = extensionExclusions;
            AsrSettings = asrSettings;
        }
        public List<string> PathExclusions { get; }
        public List<string> PolicyManagerPathExclusions { get; }
        public List<string> ProcessExclusions { get; }
        public List<string> ExtensionExclusions { get; }
        public AsrSettings AsrSettings { get; }
    }

    [CommandOutputType(typeof(WindowsDefenderDTO))]
    internal class WindowsDefenderFormatter : TextFormatterBase
    {
        public WindowsDefenderFormatter(ITextWriter writer) : base(writer)
        {
        }

        private Dictionary<string, string> _AsrGuids = new Dictionary<string, string>
            {
                { "d4f940ab-401b-4efc-aadc-ad5f3c50688a" ,"Block all Office applications from creating child processes"},
                { "5beb7efe-fd9a-4556-801d-275e5ffc04cc" , "Block execution of potentially obfuscated scripts"},
                { "92e97fa1-2edf-4476-bdd6-9dd0b4dddc7b" , "Block Win32 API calls from Office macro	"},
                { "3b576869-a4ec-4529-8536-b80a7769e899" , "Block Office applications from creating executable content	"},
                { "75668c1f-73b5-4cf0-bb93-3ecf5cb7cc84" , "Block Office applications from injecting code into other processes"},
                { "d3e037e1-3eb8-44c8-a917-57927947596d" , "Block JavaScript or VBScript from launching downloaded executable content"},
                { "be9ba2d9-53ea-4cdc-84e5-9b1eeee46550" , "Block executable content from email client and webmail"},
                { "01443614-cd74-433a-b99e-2ecdc07bfc25" , "Block executable files from running unless they meet a prevalence, age, or trusted list criteria"},
                { "c1db55ab-c21a-4637-bb3f-a12568109d35" , "Use advanced protection against ransomware"},
                { "9e6c4e1f-7d60-472f-ba1a-a39ef669e4b2" , "Block credential stealing from the Windows local security authority subsystem (lsass.exe)"},
                { "d1e49aac-8f56-4280-b9ba-993a6d77406c" , "Block process creations originating from PSExec and WMI commands"},
                { "b2b3f03d-6a65-4f7b-a9c7-1c7ef74a9ba4" , "Block untrusted and unsigned processes that run from USB"},
                { "26190899-1602-49e8-8b27-eb1d0a1ce869" , "Block Office communication applications from creating child processes"},
                { "7674ba52-37eb-4a4f-a9a1-f0f9a1619a2c" , "Block Adobe Reader from creating child processes"},
                { "e6db77e5-3df2-4cf1-b95a-636979351e5b" , "Block persistence through WMI event subscription"},
            };

        public override void FormatResult(CommandBase? command, CommandDTOBase result, bool filterResults)
        {
            var dto = (WindowsDefenderDTO)result;

            var pathExclusions = dto.PathExclusions;
            var processExclusions = dto.ProcessExclusions;
            var extensionExclusions = dto.ExtensionExclusions;
            var asrSettings = dto.AsrSettings;

            if (pathExclusions.Count != 0)
            {
                WriteLine("\r\nPath Exclusions:");
                foreach (var path in pathExclusions)
                {
                    WriteLine($"  {path}");
                }
            }

            if (pathExclusions.Count != 0)
            {
                WriteLine("\r\nPolicyManagerPathExclusions      : ");
                foreach (var path in pathExclusions)
                {
                    WriteLine($"  {path}");
                }
            }

            if (processExclusions.Count != 0)
            {
                WriteLine("\r\nProcess Exclusions");
                foreach (var process in processExclusions)
                {
                    WriteLine($"  {process}");
                }
            }

            if (extensionExclusions.Count != 0)
            {
                WriteLine("\r\nExtension Exclusions");
                foreach (var ext in extensionExclusions)
                {
                    WriteLine($"  {ext}");
                }
            }

            if (asrSettings.Enabled)
            {
                WriteLine("\r\nAttack Surface Reduction Rules:\n");

                WriteLine($"  {"State",-10} Rule\n");
                foreach (var rule in asrSettings.Rules)
                {
                    string state;
                    if (rule.State == 0)
                        state = "Disabled";
                    else if (rule.State == 1)
                        state = "Blocked";
                    else if (rule.State == 2)
                        state = "Audited";
                    else
                        state = $"{rule.State} - Unknown";

                    var asrRule = _AsrGuids.ContainsKey(rule.Rule.ToString())
                        ? _AsrGuids[rule.Rule.ToString()]
                        : $"{rule.Rule} - Please report this";

                    WriteLine($"  {state,-10} {asrRule}");
                }

                WriteLine("\nASR Exclusions:");
                foreach (var exclusion in asrSettings.Exclusions)
                {
                    WriteLine($"  {exclusion}");
                }
            }
        }
    }
}