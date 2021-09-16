using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.PacketParserIntegration;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.ActionReaction;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.IntegrationTests
{
    public class RelatedPacketsTestCaseGroup
    {
        public string SniffFilePath { get; init; } = "";
        public List<RelatedPacketsTestCase> TestCases { get; init; } = new();
    }
    
    public class RelatedPacketsTestCase
    {
        public string SearchTextStartPacket { get; init; } = "";
        public List<string> MustIncludeGuid { get; init; } = new();
        public List<string> MightIncludeGuid { get; init; } = new();
        public List<string> CurrentlyIncludeGuidButShouldNot { get; init; } = new();
        public List<string> CurrentlyNotIncludedGuidButShouldBe { get; init; } = new();
        public int? MustStartPacket { get; init; }
        public int? MustEndPacket { get; init; }
        public string TestName { get; init; } = "";
    }
    
    [AutoRegister]
    public class RelatedPacketsTester
    {
        private readonly IRelatedPacketsFinder relatedPacketsFinder;
        private readonly ISniffLoader sniffLoader;
        private readonly PacketViewModelFactory viewModelFactory;

        private List<RelatedPacketsTestCaseGroup> testCaseGroup;
        
        public RelatedPacketsTester(IRelatedPacketsFinder relatedPacketsFinder,
            ISniffLoader sniffLoader,
            PacketViewModelFactory viewModelFactory)
        {
            this.relatedPacketsFinder = relatedPacketsFinder;
            this.sniffLoader = sniffLoader;
            this.viewModelFactory = viewModelFactory;

            testCaseGroup = new List<RelatedPacketsTestCaseGroup>()
            {
                new ()
                {
                    SniffFilePath = "/Users/bartek/sniffs/7_2_5_Grizzly_Hills_Horde_Part_3.pkt",
                    TestCases = new List<RelatedPacketsTestCase>()
                    {
                        new ()
                        {
                            TestName = "'The Bear God's Offspring' Gossip",
                            SearchTextStartPacket = "You're free to go Orsonn",
                            MustStartPacket = 21994,
                            MustEndPacket = 22081,
                            MustIncludeGuid = new ()
                            {
                                "0x203CD047601AA280000006000013C64B/27274/Creature",
                                "0x08146400000000000000000009343B1E/0/Player"
                            }
                        },
                        new ()
                        {
                            TestName = "'Out of body experience, after teleport'",
                            SearchTextStartPacket = "Rise, Arugal!  The power of the Lich King commands you!",
                            MustStartPacket = 36202,
                            MustEndPacket = 36535,
                            MustIncludeGuid = new()
                            {
                                "0x20309800001AF8000036400000184CF8/27616/Creature",
                                "0x20309800001AF9000036400000184CF7/27620/Creature",
                                "0x20309800001AF8800036400000184CF8/27618/Creature",
                                "0x20309800001AF8C00036400000184CF8/27619/Creature",
                                "0x20309800001AFA000036400000184CDA/27624/Creature",
                                "0x20309800001AF9800036400000184CDA/27622/Creature",
                                "0x2C30980000B984C00036400000184CDA/189971/GameObject",
                                "0x0818F00000000000000000000A29C742/0/Player"   
                            }
                        }
                    }
                },
                new()
                {
                    SniffFilePath = "/Users/bartek/Downloads/7_2_5_Grizzly_Hills_Horde_Part_1.pkt",
                    TestCases = new List<RelatedPacketsTestCase>()
                    {
                        new()
                        {
                            TestName = "'Bringing Down the Iron Thar' reward scene",
                            SearchTextStartPacket =
                                "I'll admit, dwarf, I was pleasantly surprised to see your plan succeed.",
                            MustStartPacket = 24786,
                            MustEndPacket = 25414,
                            MustIncludeGuid = new()
                            {
                                "0x203CD047601A9540000006000013C649/27221/Creature",
                                "0x203CD047601A96C0000006000013C649/27227/Creature",
                                "0x203CD047601A0A80000006000013C649/26666/Creature"
                            },
                            MightIncludeGuid = new()
                            {
                                "0x0818F00000000000000000000A29C742/0/Player",
                            }
                        },
                        new()
                        {
                            TestName = "'The Damaged Journal' reward scene",
                            SearchTextStartPacket =
                                "This is an intriguing find, $n, but I don't know what to make of it.",
                            MustStartPacket = 51261,
                            MustEndPacket = 51794,
                            MustIncludeGuid = new()
                            {
                                "0x203CD0476019F600000006000013C649/26584/Creature",
                                "0x203CD047601A0A80000006000013C649/26666/Creature",
                            },
                            MightIncludeGuid = new()
                            {
                                "0x0818F00000000000000000000A29C742/0/Player",
                            },
                            CurrentlyIncludeGuidButShouldNot = new()
                            {
                                "0x203CD047601C9D400000060000979F02/29301/Creature",
                                "0x203CD047601C9D400000060000179F02/29301/Creature"
                            }
                        },
                        new()
                        {
                            TestName = "'Deciphering the Journal' reward scene",
                            SearchTextStartPacket = "Let us see what this journal reveals.",
                            MustStartPacket = 52971,
                            MustEndPacket = 53600, // should be 53315
                            MustIncludeGuid = new List<string>()
                            {
                                "0x203CD0476019F600000006000013C649/26584/Creature",
                                "0x2C3CD04760B807000000060000179FE5/188444/GameObject",
                            },
                            MightIncludeGuid = new List<string>()
                            {
                                "0x0818F00000000000000000000A29C742/0/Player"
                            },
                            CurrentlyIncludeGuidButShouldNot = new List<string>()
                            {
                                "0x203CD047601A14C0000006000013C65A/26707/Creature", // completely unrelated, could be easily removed if there was a way to globally detect unrelated actors
                                "0x2C3CD04760B806C00000060000179FE2/188443/GameObject"
                            }
                        },
                        new()
                        {
                            TestName = "'Loken's Orders' quest scene",
                            SearchTextStartPacket = "You're late, overseer.",
                            MustStartPacket = 104834,
                            MustEndPacket = 105003,
                            MustIncludeGuid = new()
                            {
                                "0x2C3CD04760B82D00000006000013C64B/188596/GameObject",
                                "0x0818F00000000000000000000A29C742/0/Player",
                                "0x203CD047601A9300000006000017A842/27212/Creature"
                            }
                        },
                        new()
                        {
                            TestName = "'A Bear of an Appetite' reward scene",
                            SearchTextStartPacket = "By the Light, you killed Limpy Joe and took the meat!",
                            MustStartPacket = 68037,
                            MustEndPacket = 68328,
                            MustIncludeGuid = new()
                            {
                                "0x203CD0476019DD00000006000013C647/26484/Creature",
                                "0x0818F00000000000000000000A29C742/0/Player"
                            },
                            CurrentlyIncludeGuidButShouldNot = new()
                            {
                                "0x2C3CD04760B81600000006000017A1EF/188504/GameObject"
                            }
                        },
                        new()
                        {
                            TestName = "'In the Name of Loken': gossip with Hugh Glass + text",
                            SearchTextStartPacket = "That's not something Limpy Joe would ask!",
                            MustStartPacket = 60329,
                            MustEndPacket = 60396,
                            MustIncludeGuid = new()
                            {
                                "0x203CD0476019DD00000006000013C647/26484/Creature",
                                "0x0818F00000000000000000000A29C742/0/Player"
                            }
                        },
                        new()
                        {
                            TestName = "'In the Name of Loken': gossip with Gavrock + text",
                            SearchTextStartPacket = "Ah, yes. Loken is well known to me.",
                            MustStartPacket = 61356,
                            MustEndPacket = 61448,
                            MightIncludeGuid = new()
                            {
                                "0x203CD0476019CD00000006000013C647/26420/Creature",
                                "0x0818F00000000000000000000A29C742/0/Player"
                            }
                        },
                        new()
                        {
                            TestName = "Dun-da-Dun-tah! escort [NPCS missing in this scene]",
                            SearchTextStartPacket = "Alright, kid. Stay behind me and you'll be fine.",
                            MustStartPacket = 73864,
                            MustEndPacket = 77351,
                            MustIncludeGuid = new()
                            {
                                "0x203CD047601A2F8000000600001790C5/26814/Creature", // Harrison Jones - main escort unit
                                "0x2C3CD04760B80C40000006000013C649/188465/GameObject", // Harrison's cage
                                "0x203CD0476017D540000006000017A298/24405/Creature", // Adarrah - in side scene
                                "0x203CD047601A3C40000006000017A2FE/26865/Creature", // Tecahuna - npc we are fighting with
                                "0x2C3CD04760B811C0000006000013C65B/188487/GameObject", // Adarra's Cage
                                "0x0818F00000000000000000000A29C742/0/Player",

                            },
                            MightIncludeGuid = new()
                            {
                                "0x203CD047601A2F00000006000017A297/26812/Creature", // npcs he fight with during the escort
                                "0x203CD047601A2D800000060000179D4B/26806/Creature",
                                "0x203CD047601A2D800000060000179FC1/26806/Creature",
                                "0x203CD047601A2AC00000060000179E40/26795/Creature",
                                "0x203CD047601A2B400000060000179DFB/26797/Creature",
                                "0x203CD047601A2B400000060000179E1D/26797/Creature"
                            },
                            CurrentlyNotIncludedGuidButShouldBe = new()
                            {
                                "0x203CD047601A3CC0000006000097A2E6/26867/Creature", // mummies that only start a fire when the event beings, really hard to connect to the main event
                                "0x203CD047601A3CC0000006000117A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000197A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000217A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000297A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000317A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000397A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000417A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000497A2E6/26867/Creature",
                                "0x203CD047601A3CC0000006000017A2E6/26867/Creature",
                                "0x2C3CD04760B81000000006000017A2FA/188480/GameObject" // firedoor that are blocking the way out during the event, it is really hard to connect spawn of this object with the event tho
                            }
                        },
                        new()
                        {
                            TestName =
                                "'See You on The Other Side' gong + corpse (doesn't include reward, tho it could)",
                            SearchTextStartPacket = "How dare you summon me without an offering!",
                            MustStartPacket = 82320,
                            MustEndPacket = 82591,
                            MustIncludeGuid = new()
                            {
                                "0x0818F00000000000000000000A29C742/0/Player",
                                "0x203CD047601A4580000006000017A418/26902/Creature", // essence
                                "0x203CD047601FF980000006000017A422/32742/Creature", // corpse
                                "0x203CD047601A4E40000006000093C65B/26937/Creature", // bunny
                                "0x203CD047601A4E40000006000013C65B/26937/Creature", // bunny
                                "0x203CD047601A4E40000006000013C64B/26937/Creature", // bunny
                                "0x203CD047601A4E40000006000013C65A/26937/Creature" // bunny
                            }
                        }
                    }
                }
            };
        }

        public async Task TestCase()
        {
            foreach (var test in testCaseGroup)
                await SingleTestCase(test);
        }
        
        public async Task SingleTestCase(RelatedPacketsTestCaseGroup testCaseGroup)
        {
            var sniff = await sniffLoader.LoadSniff(testCaseGroup.SniffFilePath, CancellationToken.None, new Progress<float>());
            var splitter = new SplitUpdateProcessor(new GuidExtractorProcessor());
            List<PacketViewModel> split = new();
            foreach (var packet in sniff.Packets_)
            {
                var output = splitter.Process(packet);
                if (output != null)
                    foreach (var p in output)
                        if (viewModelFactory.Process(p) is PacketViewModel pvm)
                            split.Add(pvm);
            }

            var finalized = splitter.Finalize();
            if (finalized != null)
                foreach (var p in finalized)
                    if (viewModelFactory.Process(p) is PacketViewModel pvm)
                        split.Add(pvm);
            
            foreach (var group in testCaseGroup.TestCases)
            {
                Console.WriteLine("\n\n" + group.TestName);
                var startPackets = FindPacket(split, group.SearchTextStartPacket);
                var result = relatedPacketsFinder.Find(split, startPackets!.Id, CancellationToken.None);

                var mustIncludeGuids = group.MustIncludeGuid.StringToGuids().ToList();
                var mayIncludeGuids = group.MightIncludeGuid.StringToGuids().ToList();
                var includesButShouldNot = group.CurrentlyIncludeGuidButShouldNot.StringToGuids().ToList();
                var notIncludesButShould = group.CurrentlyNotIncludedGuidButShouldBe.StringToGuids().ToList();

                if (group.MustStartPacket.HasValue && (!result.MinPacketNumber.HasValue ||
                                                       group.MustStartPacket.Value != result.MinPacketNumber.Value))
                    throw new Exception("Invalid start packet");
                
                if (group.MustEndPacket.HasValue && (!result.MaxPacketNumber.HasValue ||
                                                       group.MustEndPacket.Value != result.MaxPacketNumber.Value))
                    throw new Exception("Invalid end packet");
                
                if (mustIncludeGuids.Any(guid =>
                    !(result.IncludedGuids ?? new HashSet<UniversalGuid>()).Contains(guid)))
                    throw new Exception("Some guid is not present!");
                
                if (result.IncludedGuids != null &&
                    result.IncludedGuids.Any(guid => !mustIncludeGuids.Contains(guid) && 
                                                     !mayIncludeGuids.Contains(guid) &&
                                                     !includesButShouldNot.Contains(guid) && 
                                                     !notIncludesButShould.Contains(guid)))
                    throw new Exception("Too many guids are present!");

                foreach (var unnecessary in includesButShouldNot)
                {
                    if (result.IncludedGuids != null && !result.IncludedGuids.Contains(unnecessary))
                        Console.WriteLine(unnecessary.ToWowParserString() + " is no longer included, that's a success!");
                }
                
                foreach (var necessary in notIncludesButShould)
                {
                    if (result.IncludedGuids != null && result.IncludedGuids.Contains(necessary))
                        Console.WriteLine(necessary.ToWowParserString() + " is now included, that's a success!");
                }
                
            }
        }

        private PacketViewModel? FindPacket(ICollection<PacketViewModel> packets, string text)
        {
            foreach (var p in packets)
            {
                if (p.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    return p;
            }

            return null;
        }
    }
    
    /*
     
     
     
     {
  "MinPacket": 51261,
  "MaxPacket": 51794,
  "IncludedGuids": [
  ],
  "ForceIncludedNumbers": [
    51267,
    51501
  ]
}
     *
     * {
  "MinPacket": 24786,
  "MaxPacket": 25414,
  "IncludedGuids": [
    "0x203CD047601A9540000006000013C649/27221/Creature",
    "0x0818F00000000000000000000A29C742/0/Player",
    "0x203CD047601A96C0000006000013C649/27227/Creature",
    "0x203CD047601A0A80000006000013C649/26666/Creature"
  ],
  "ForceIncludedNumbers": [
    24819
  ]
}

     */
}