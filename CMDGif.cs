using MonoMod.ModInterop;
using MonoMod.Utils;
using Quintessential;
using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;

namespace CMDGif
{   
    using BondType = enum_126;
    using BondStyle = class_200;
    using BondStyles = class_167; 

    public enum FramingMode
    {
        Default,
        Equilibrium,
        Bounds,
    }

    public class CMDGif : QuintessentialMod
    {
        public string solution_name = "";
        public int start_cycle = -1;
        public int end_cycle = -1;
        public int frames_per_cycle = 6;
        public int speed = 5;

        public Bounds2 bound = Bounds2.Empty;

        public FramingMode framing = FramingMode.Default;

        public void class250_ctor(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if(
                cursor.TryGotoNext(MoveType.After, instr => instr.Match(OpCodes.Ldarg_1))
                )
            {
                // Fucking anhilate this part of the function
                cursor.RemoveRange(8);
                // Cursed spot
                cursor.EmitDelegate<Func<Solution, Bounds2>>((Solution solution) => {
                    Bounds2 result= Bounds2.Empty;
                    switch(framing)
                    {
                        case FramingMode.Default:
                            if (solution.method_1934().field_2779.method_1085())
                            {
                                result = solution.method_1934().field_2779.method_1087().field_2074.Expanded(125f, 125f, 125f, 75f);
                                break;
                            }
                            result = solution.method_1955().Expanded(75f, 75f);
                            break;
                        case FramingMode.Equilibrium:
                            if (solution.method_1934().field_2779.method_1085())
                            {
                                result = solution.method_1934().field_2779.method_1087().field_2074.Expanded(125f, 125f, 125f, 75f);
                                break;
                            }
                            HashSet<HexIndex> hashSet = new HashSet<HexIndex>();
                            foreach(Part part in solution.method_1937())
                            {
                                if (part.method_1159().field_1554 || part.method_1159() != class_191.field_1782)
                                {
                                    continue;
                                }
                                HashSet<HexIndex> other = part.method_1186(solution);
                                hashSet.UnionWith(other);
                            }
                            bool flag = true;
                            foreach (HexIndex item2 in hashSet)
                            {
                                Vector2 vector = class_187.field_1742.method_491(item2, Vector2.Zero);
                                if (flag)
                                {
                                    result= Bounds2.WithCorners(vector, vector);
                                    flag = false;
                                }
                                else
                                {
                                    result = result.UnionedWith(vector);
                                }
                            }

                            // If there's no eq glyphs just use the default
                            if(result.Min.X == 0 && result.Min.Y == 0 && result.Max.X == 0 && result.Max.Y == 0)
                            {
                                result = solution.method_1955().Expanded(75f, 75f);
                            }
                            break;
                        case FramingMode.Bounds:
                            result = Bounds2.WithCorners(0f, 0f, 69f, 420f);
                            break;
                        default:
                            new Exception("Undefined result mode... How'd you do this?");
                            break;
                    }
                    return result;
                });
            } else {
                throw new Exception("Failed to modify bounds (Couldn't find solution loading)");
            }
        }

        public override void Load() {
            // IL.GameLogic.method_956 += OnLoadSingletons;
            IL.class_250.ctor += class250_ctor;

            foreach(string arg in Environment.GetCommandLineArgs())
            {
                if(arg.EndsWith(".solution"))
                {
                    Logger.Log("CMDGif: Found solution arg");
                    solution_name = arg;
                } else if(arg.StartsWith("start=")) {
                    if(!int.TryParse(arg.Split('=')[1], out start_cycle)) {
                        Logger.Log("CMDGif: Error, start cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("end=")) {
                    if(!int.TryParse(arg.Split('=')[1], out end_cycle)) {
                        Logger.Log("CMDGif: Error, end cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("fpc=")) {
                    if(!int.TryParse(arg.Split('=')[1], out frames_per_cycle)) {
                        Logger.Log("CMDGif: Error, frames per cycle not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("speed=")) {
                    if(!int.TryParse(arg.Split('=')[1], out speed)) {
                        Logger.Log("CMDGif: Error, speed not set to valid integer!");
                        GameLogic.field_2434.method_963(0);
                    }
                } else if(arg.StartsWith("framing=")) {
                    switch(arg.Split('=')[1])
                    {
                        case "eq":
                            framing = FramingMode.Equilibrium;
                            break;
                        case "bounds":
                            framing = FramingMode.Bounds;
                            break;
                        default:
                            framing = FramingMode.Default;
                            break;
                    }
                } else if(arg.StartsWith("min=")) {
                    var x = arg.Split('=')[1];
                }
            }

            On.class_250.method_50 += (orig, self, param) =>
            {
                orig(self, param);
                if(!(bool)typeof(class_250).GetField("field_2026", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self)) return;
                foreach(string arg in Environment.GetCommandLineArgs())
                {
                    if(arg.EndsWith(".solution"))
                    {
                        Logger.Log("CMDGif: Successfully created, closing the game...");
                        GameLogic.field_2434.method_963(0);
                    }
                }
            };
        }

        public override void LoadPuzzleContent() {
        }

        public override void PostLoad() {
            if(solution_name != "")
            {
                Maybe<Solution> maybeSolution = Solution.method_1958(solution_name);
                class_162.method_403(maybeSolution.method_1085(), "Failed to load solution file.");
                Solution solution = maybeSolution.method_1087();
                var class250Type = typeof(class_250);
                // Thanks, GuiltyBystander!
                DynData<class_250> class250 = (new DynData<class_250>(null));
                
                if(frames_per_cycle > 0) {
                    class250["field_2017"] = frames_per_cycle;
                }

                if(speed > 0) {
                    class250["field_2018"] = speed;
                }
                
                class_250 gifScreen = new class_250(solution);
                
                if(start_cycle > -1 && end_cycle > start_cycle) {
                    class250Type.GetField("field_2028", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, start_cycle);
                    class250Type.GetField("field_2029", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, end_cycle);
                    class250Type.GetField("field_2030", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(gifScreen, true);
                }
                GameLogic.field_2434.method_946(gifScreen);
            }
        }
        

        public override void Unload() {
            // IL.GameLogic.method_956 -= OnLoadSingletons;
            IL.class_250.ctor -= class250_ctor;
        }
    }
}