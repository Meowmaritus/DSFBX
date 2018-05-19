using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeowDSIO.DataTypes.TAE;
using System.ComponentModel;
using Newtonsoft.Json;

namespace MeowDSIO.DataFiles
{
    
    public class TAE : DataFile
    {
        public IEnumerable<int> AnimationIDs => Animations.Select(x => x.ID);

        public void UpdateAnimGroupIndices()
        {
            for (int i = 0; i < AnimationGroups.Count; i++)
            {
                AnimationGroups[i].DisplayIndex = i + 1;
            }
        }

        public TAEHeader Header { get; set; }
        public string SkeletonName { get; set; }
        public string SibName { get; set; }
        public List<AnimationRef> Animations { get; set; } = new List<AnimationRef>();
        public List<AnimationGroup> AnimationGroups { get; set; } = new List<AnimationGroup>();

        public int EventHeaderSize
        {
            get
            {
                if (Header.VersionMajor == 11 && Header.VersionMinor == 1)
                    return 0x0C;
                else if (Header.VersionMajor == 1 && Header.VersionMinor == 0)
                    return 0x10;
                else
                    throw new NotImplementedException($"Don't know event header size of TAE Version {Header.VersionMajor}.{Header.VersionMinor}");
            }
        }

        private Animation LoadAnimationFromOffset(DSBinaryReader bin, int offset, int animID_ForDebug)
        {
            int oldOffset = (int)bin.BaseStream.Position;
            bin.BaseStream.Seek(offset, SeekOrigin.Begin);
            var anim = new Animation();

            try
            {
                int eventCount = bin.ReadInt32();
                int eventHeadersOffset = bin.ReadInt32();
                bin.BaseStream.Seek(0x10, SeekOrigin.Current); //skip shit we don't need
                int animFileOffset = bin.ReadInt32();

                for (int i = 0; i < eventCount; i++)
                {
                    //lazily seek to the start of each event manually.
                    bin.BaseStream.Seek(eventHeadersOffset + (EventHeaderSize * i), SeekOrigin.Begin);

                    int startTimeOffset = bin.ReadInt32();
                    int endTimeOffset = bin.ReadInt32();
                    int eventBodyOffset = bin.ReadInt32();

                    bin.BaseStream.Seek(startTimeOffset, SeekOrigin.Begin);
                    float startTime = bin.ReadSingle();
                    bin.BaseStream.Seek(endTimeOffset, SeekOrigin.Begin);
                    float endTime = bin.ReadSingle();
                    bin.BaseStream.Seek(eventBodyOffset, SeekOrigin.Begin);
                    int eventTypeValue = bin.ReadInt32();
                    int eventParamOffset = bin.ReadInt32();
                    bin.BaseStream.Seek(eventParamOffset, SeekOrigin.Begin);

                    AnimationEventType eventType = (AnimationEventType)eventTypeValue;
                    var nextEvent = new AnimationEvent(i + 1, eventType, animID_ForDebug);

                    nextEvent.StartTime = startTime;
                    nextEvent.EndTime = endTime;

                    for (int j = 0; j < nextEvent.Parameters.Count; j++)
                    {
                        var nextParamVal = new MultiDword() { Int = bin.ReadInt32() };

                        if (AnimationEvent.CheckIfParamIsUnlikelyToBeFloat(nextParamVal))
                        {
                            nextEvent.Parameters[j].Value = nextParamVal.Int.ToString();
                        }
                        else
                        {
                            string convStr = nextParamVal.Float.ToString();
                            nextEvent.Parameters[j].Value = convStr + (convStr.Contains(".") ? "" : ".0");
                        }
                    }

                    anim.Events.Add(nextEvent);
                }

                bin.BaseStream.Seek(animFileOffset, SeekOrigin.Begin);

                int fileType = bin.ReadInt32();
                if (fileType == 0)
                {
                    int dataOffset = bin.ReadInt32();
                    bin.BaseStream.Seek(dataOffset, SeekOrigin.Begin);

                    int nameOffset = bin.ReadInt32();
                    if (nameOffset == 0)
                    {
                        throw new Exception("Anim file type was that of a named one but the name pointer was NULL.");
                    }
                    bin.BaseStream.Seek(nameOffset, SeekOrigin.Begin);
                    anim.FileName = ReadUnicodeString(bin);
                }
                else if (fileType == 1)
                {
                    anim.FileName = null;
                }
                else
                {
                    throw new Exception($"Unknown anim file type code: {fileType}");
                }

                return anim;
            }
            catch (EndOfStreamException)
            {
                MiscUtil.PrintlnDX($"Warning: reached end of file while parsing animation {animID_ForDebug}; data may not be complete.", ConsoleColor.Yellow);
                //if (!MiscUtil.ConsolePrompYesNo("Would you like to continue loading the file and run the risk of " + 
                //    "accidentally outputting a file that might be missing some of its original data?"))
                //{
                //    throw new LoadAbortedException();
                //}
                //else
                //{
                //    return a;
                //}

                return anim;
            }
            finally
            {
                bin.BaseStream.Seek(oldOffset, SeekOrigin.Begin);
            }
        }

        private string ReadUnicodeString(DSBinaryReader bin)
        {
            StringBuilder sb = new StringBuilder();
            byte[] next = { 0, 0 };
            bool endString = false;
            do
            {
                next = bin.ReadBytes(2);
                endString = (next[0] == 0 && next[1] == 0);

                if (!endString)
                {
                    sb.Append(Encoding.Unicode.GetString(next));
                }
            }
            while (!endString);
            return sb.ToString();
        }

        //TODO: Measure real progress.
        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            prog?.Report((0, 1)); //PLACEHOLDER

            Header = new TAEHeader();
            var fileSignature = bin.ReadBytes(4);
            if (fileSignature.Where((x, i) => x != Header.Signature[i]).Any())
            {
                throw new Exception($"Invalid signature in this TAE file: " + 
                    $"[{string.Join(",", fileSignature.Select(x => x.ToString("X8")))}] " + 
                    $"(ASCII: '{Encoding.ASCII.GetString(fileSignature)}')");
            }

            Header.IsBigEndian = bin.ReadBoolean();

            bin.BigEndian = Header.IsBigEndian;
            bin.ReadBytes(3); //3 null bytes after big endian flag

            Header.VersionMajor = bin.ReadUInt16();
            Header.VersionMinor = bin.ReadUInt16();

            var fileSize = bin.ReadInt32();

            Header.UnknownB00 = bin.ReadUInt32();
            Header.UnknownB01 = bin.ReadUInt32();
            Header.UnknownB02 = bin.ReadUInt32();
            Header.UnknownB03 = bin.ReadUInt32();

            Header.UnknownFlags = bin.ReadBytes(TAEHeader.UnknownFlagsLength);

            Header.FileID = bin.ReadInt32();
            
            //Animation IDs
            var animCount = bin.ReadInt32();
            int OFF_AnimID = bin.ReadInt32();
            bin.DoAt(OFF_AnimID, () =>
            {
                for (int i = 0; i < animCount; i++)
                {
                    var animID = bin.ReadInt32();
                    var animOffset = bin.ReadInt32();
                    var anim = LoadAnimationFromOffset(bin, animOffset, animID);
                    var animRef = new AnimationRef() { ID = animID, Anim = anim };

                    Animations.Add(animRef);
                }
            });

            //Anim Groups
            int OFF_AnimGroups = bin.ReadInt32();
            bin.DoAt(OFF_AnimGroups, () =>
            {
                int animGroupCount = bin.ReadInt32();
                int actualAnimGroupsOffset = bin.ReadInt32();

                bin.BaseStream.Seek(actualAnimGroupsOffset, SeekOrigin.Begin);

                for (int i = 0; i < animGroupCount; i++)
                {
                    var nextAnimGroup = new AnimationGroup(i + 1);
                    nextAnimGroup.FirstID = bin.ReadInt32();
                    nextAnimGroup.LastID = bin.ReadInt32();
                    var _firstIdOffset = bin.ReadInt32();

                    AnimationGroups.Add(nextAnimGroup);
                }
            });

            Header.UnknownC = bin.ReadInt32();
            //We already found the animation count and offsets from the anim ids earlier
            int _animCount = bin.ReadInt32();
            int _animOffset = bin.ReadInt32();
            if (_animCount != Animations.Count)
            {
                throw new Exception($"Animation IDs count [{Animations.Count}] is different than Animations count [{_animCount}]!");
            }

            Header.UnknownE00 = bin.ReadUInt32();
            Header.UnknownE01 = bin.ReadUInt32();
            Header.UnknownE02 = bin.ReadUInt32();
            Header.UnknownE03 = bin.ReadUInt32();
            Header.UnknownE04 = bin.ReadUInt32();
            Header.FileID2 = bin.ReadInt32();
            Header.FileID3 = bin.ReadInt32();
            Header.UnknownE07 = bin.ReadUInt32();
            Header.UnknownE08 = bin.ReadUInt32();
            Header.UnknownE09 = bin.ReadUInt32();

            int filenamesOffset = bin.ReadInt32();
            bin.BaseStream.Seek(filenamesOffset, SeekOrigin.Begin);

            int skeletonNameOffset = bin.ReadInt32();
            int sibNameOffset = bin.ReadInt32();

            bin.BaseStream.Seek(skeletonNameOffset, SeekOrigin.Begin);

            SkeletonName = ReadUnicodeString(bin);

            bin.BaseStream.Seek(sibNameOffset, SeekOrigin.Begin);

            SibName = ReadUnicodeString(bin);

            prog?.Report((1, 1)); //PLACEHOLDER
        }

        private void Println(string txt)
        {
            Console.WriteLine(txt);
        }

        //TODO: Measure real progress.
        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            prog?.Report((0, 1)); //PLACEHOLDER

            //SkeletonName, SibName:
            bin.Seek(0x94, SeekOrigin.Begin);
            bin.Write(0x00000098);
            bin.Write(0x000000A8);
            bin.Write(0x000000A8 + (SkeletonName.Length * 2 + 2));
            bin.Write(0x00000000);
            bin.Write(0x00000000);
            bin.Write(Encoding.Unicode.GetBytes(SkeletonName));
            bin.Write((short)0); //string terminator
            bin.Write(Encoding.Unicode.GetBytes(SibName));
            bin.Write((short)0); //string terminator

            //Animation IDs - First Pass
            int OFF_AnimationIDs = (int)bin.BaseStream.Position;

            var animationIdOffsets = new Dictionary<int, int>(); //<animation ID, offset>
            foreach (var anim in Animations)
            {
                animationIdOffsets.Add(anim.ID, (int)bin.BaseStream.Position);
                bin.Write(anim.ID);
                bin.Placeholder(); //Pointer to animation will be inserted here.
            }

            //Animation Groups - Full Pass
            int OFF_AnimationGroups = (int)bin.BaseStream.Position;
            bin.Write(AnimationGroups.Count);
            bin.Write((int)(bin.BaseStream.Position + 4)); //Pointer that always points to the next dword for some reason.

            foreach (var g in AnimationGroups)
            {
                bin.Write(g.FirstID);
                bin.Write(g.LastID);

                if (!animationIdOffsets.ContainsKey(g.FirstID))
                    throw new Exception($"Animation group begins on an animation ID that isn't " + 
                        $"present in the list of animations.");

                bin.Write(animationIdOffsets[g.FirstID]);
            }

            //Animation First Pass
            int OFF_Animations = (int)bin.BaseStream.Position;
            var animationOffsets = new Dictionary<int, int>(); //<animation ID, offset>
            var animationTimeConstantLists = new Dictionary<int, List<float>>(); //<animation ID, List<time constant>>
            foreach (var anim in Animations)
            {
                animationOffsets.Add(anim.ID, (int)bin.BaseStream.Position);
                bin.Write(anim.Anim.Events.Count);
                bin.Placeholder(); //PLACEHOLDER: animation event headers offset
                //Println($"Wrote Anim{anim.Key} event header offset placeholder value (0xDEADD00D) at address {(bin.BaseStream.Position-4):X8}");
                bin.Write(0); //Null 1
                bin.Write(0); //Null 2
                animationTimeConstantLists.Add(anim.ID, new List<float>());
                //Populate all of the time constants used:
                foreach (var e in anim.Anim.Events)
                {
                    if (!animationTimeConstantLists[anim.ID].Contains(e.StartTime))
                        animationTimeConstantLists[anim.ID].Add(e.StartTime);
                    if (!animationTimeConstantLists[anim.ID].Contains(e.EndTime))
                        animationTimeConstantLists[anim.ID].Add(e.EndTime);
                }
                bin.Write(animationTimeConstantLists[anim.ID].Count); //# time constants in this anim
                bin.Placeholder(); //PLACEHOLDER: Time Constants offset
                //Println($"Wrote Anim{anim.Key} time constant offset placeholder value (0xDEADD00D) at address {(bin.BaseStream.Position-4):X8}");
                bin.Placeholder(); //PLACEHOLDER: Animation file struct offset
                //Println($"Wrote Anim{anim.Key} anim file offset placeholder value (0xDEADD00D) at address {(bin.BaseStream.Position-4):X8}");
            }



            //Animation ID Second Pass
            bin.DoAt(OFF_AnimationIDs, () =>
            {
                foreach (var anim in Animations)
                {
                    //Move from the animation ID offset to the animation pointer offset.
                    bin.Jump(4);

                    //Write pointer to animation into this animation ID.
                    bin.Write(animationOffsets[anim.ID]);
                }
            });


            //Animations Second Pass
            var eventHeaderStartOffsets = new Dictionary<int, int>(); //<anim ID, event header start offset>
            var animationFileOffsets = new Dictionary<int, int>(); //<animation ID, offset>
            var animTimeConstantStartOffsets = new Dictionary<int, int>(); //<animation ID, start offset>
            var animTimeConstantOffsets = new Dictionary<int, Dictionary<float, int>>(); //<animation ID, Dictionary<time const, offset>>
            {
                //The unnamed animation files contain the ID of the last named animation file.
                int lastNamedAnimation = -1;

                //TODO: Check if it's possible for the very first animation data listed to be unnamed, 
                //and what would go in the last named animation ID value if that were the case?

                foreach (var anim in Animations)
                {
                    //ANIMATION FILE:
                    {
                        //Write anim file struct:
                        animationFileOffsets.Add(anim.ID, (int)bin.BaseStream.Position);
                        if (anim.Anim.FileName != null)
                        {
                            lastNamedAnimation = anim.ID; //Set last named animation ID value for the unnamed ones to reference.
                            bin.Write(0x00000000); //type 0 - named
                            bin.Write((int)(bin.BaseStream.Position + 0x04)); //offset pointing to next dword for some reason.
                            bin.Write((int)(bin.BaseStream.Position + 0x10)); //offset pointing to name start
                            bin.Write(anim.Anim.Unk1); //Unknown
                            bin.Write(anim.Anim.Unk2); //Unknown
                            bin.Write(0x00000000); //Null
                            //name start:
                            bin.Write(Encoding.Unicode.GetBytes(anim.Anim.FileName));
                            bin.Write((short)0); //string terminator
                        }
                        else
                        {
                            bin.Write(0x00000001); //type 1 - nameless
                            bin.Write((int)(bin.BaseStream.Position + 0x04)); //offset pointing to next dword for some reason.
                            bin.Write((int)(bin.BaseStream.Position + 0x14)); //offset pointing to start of next anim file struct
                            bin.Write(lastNamedAnimation); //Last named animation ID, to which this one is linked.
                            bin.Write(0x00000000); //Null 1
                            bin.Write(0x00000000); //Null 2
                            bin.Write(0x00000000); //Null 3
                        }
                    }
                    animTimeConstantStartOffsets.Add(anim.ID, (int)bin.BaseStream.Position);
                    //Write the time constants and record their offsets:
                    animTimeConstantOffsets.Add(anim.ID, new Dictionary<float, int>());
                    foreach (var tc in animationTimeConstantLists[anim.ID])
                    {
                        animTimeConstantOffsets[anim.ID].Add(tc, (int)bin.BaseStream.Position);
                        bin.Write(tc);
                    }


                    //Event headers (note: all event headers are (EventHeaderSize) long):
                    eventHeaderStartOffsets.Add(anim.ID, (int)bin.BaseStream.Position);
                    foreach (var e in anim.Anim.Events)
                    {
                        long currentEventHeaderStart = bin.Position;
                        bin.Write((int)animTimeConstantOffsets[anim.ID][e.StartTime]); //offset of start time in time constants.
                        bin.Write((int)animTimeConstantOffsets[anim.ID][e.EndTime]); //offset of end time in time constants.
                        bin.Placeholder(); //PLACEHOLDER: Event body
                        long currentEventHeaderLength = bin.Position - currentEventHeaderStart;
                        while (currentEventHeaderLength < EventHeaderSize)
                        {
                            bin.Write((byte)0);
                        }
                    }

                    //Event bodies
                    var eventBodyOffsets = new Dictionary<AnimationEvent, int>();
                    foreach (var e in anim.Anim.Events)
                    {
                        eventBodyOffsets.Add(e, (int)bin.BaseStream.Position);

                        bin.Write((int)e.Type);
                        bin.Write((int)(bin.BaseStream.Position + 4)); //one of those pointers to very next dword.

                        //Note: the logic for the length of a particular event param array is handled 
                        //      in the read function as well as in the AnimationEvent class itself.

                        foreach (var p in e.Parameters)
                        {
                            var paramVal = p.Value.ToUpper();
                            if (paramVal.EndsWith("F") || paramVal.Contains(".") || paramVal.Contains(","))
                            {
                                bin.Write(float.Parse(paramVal.Replace("F", "")));
                            }
                            else
                            {
                                bin.Write(int.Parse(paramVal));
                            }
                        }
                    }

                    //Event headers pass 2:
                    bin.DoAt(eventHeaderStartOffsets[anim.ID], () =>
                    {
                        foreach (var e in anim.Anim.Events)
                        {
                            //skip to event body offset field:
                            bin.Seek(8, SeekOrigin.Current);

                            //write event body offset:
                            bin.Write(eventBodyOffsets[e]);
                        }
                    });

                }
            }

            //Animations Third Pass
            bin.DoAt(OFF_Animations, () =>
            {
                foreach (var anim in Animations)
                {
                    bin.Seek(animationOffsets[anim.ID] + 4, SeekOrigin.Begin);
                    //event header start offset:
                    if (anim.Anim.Events.Count > 0)
                        bin.Write(eventHeaderStartOffsets[anim.ID]);
                    else
                        bin.Write(0x00000000);
                    //Println($"Wrote Anim{anim.Key} event header offset value 0x{eventHeaderStartOffsets[anim.Key]:X8} at address {(bin.BaseStream.Position-4):X8}");
                    bin.Seek(0xC, SeekOrigin.Current);
                    //time constants offset (writing offset of the *first constant listed*):
                    if (animationTimeConstantLists[anim.ID].Count > 0)
                    {
                        bin.Write(animTimeConstantStartOffsets[anim.ID]);
                        //Println($"Wrote Anim{anim.Key} time constants offset value 0x{animTimeConstantStartOffsets[anim.Key]:X8} at address {(bin.BaseStream.Position-4):X8}");
                    }
                    else
                    {
                        bin.Write(0x00000000); //Null
                        //Println($"Wrote Anim{anim.Key} time constants offset value NULL at address {(bin.BaseStream.Position-4):X8}");
                    }
                    //anim file struct offset:
                    bin.Write((int)animationFileOffsets[anim.ID]);
                    //Println($"Wrote Anim{anim.Key} anim file offset value 0x{((int)animationFileOffsets[anim.Key]):X8} at address {(bin.BaseStream.Position-4):X8}");
                }
            });

            //final header write:
            bin.Seek(0, SeekOrigin.Begin);

            bin.Write(Header.Signature);

            bin.Write(Header.IsBigEndian);
            bin.BigEndian = Header.IsBigEndian;
            bin.Write(new byte[] { 0, 0, 0 }); //3 null bytes after big endian flag.

            bin.Write(Header.VersionMajor);
            bin.Write(Header.VersionMinor);

            bin.Write((int)bin.BaseStream.Length); //File length

            bin.Write(Header.UnknownB00);
            bin.Write(Header.UnknownB01);
            bin.Write(Header.UnknownB02);
            bin.Write(Header.UnknownB03);

            bin.Write(Header.UnknownFlags);

            bin.Write(Header.FileID);
            bin.Write(Animations.Count); //Technically the animation ID count
            bin.Write(OFF_AnimationIDs);
            bin.Write(OFF_AnimationGroups);
            bin.Write(Header.UnknownC);
            bin.Write(Animations.Count); //Techically the animation count
            bin.Write(OFF_Animations);

            bin.Write(Header.UnknownE00);
            bin.Write(Header.UnknownE01);
            bin.Write(Header.UnknownE02);
            bin.Write(Header.UnknownE03);
            bin.Write(Header.UnknownE04);
            bin.Write(Header.FileID2);
            bin.Write(Header.FileID3);
            bin.Write(Header.UnknownE07);
            bin.Write(Header.UnknownE08);
            bin.Write(Header.UnknownE09);

            //Here would be the value at the very beginning of this method!

            //Go to end and pretend like this was a totally normal file write and not the shitshow it was.
            bin.Seek(0, SeekOrigin.End);

            prog?.Report((1, 1)); //PLACEHOLDER
        }
    }
}
