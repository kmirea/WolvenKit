using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using WolvenKit.Common;
using WolvenKit.Common.Extensions;
using WolvenKit.Common.FNV1A;
using WolvenKit.Common.Oodle;
using WolvenKit.Common.Services;
using WolvenKit.Modkit.RED4.GeneralStructs;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.CR2W.Archive;
using WolvenKit.RED4.CR2W.Reflection;
using WolvenKit.RED4.CR2W.Types;


namespace WolvenKit.Modkit.RED4.Animation
{
    public class Anim_Event
    {
        public string parentAnim { get; set; }
        public int startFrame { get; set; }
        public int durationInFrames { get; set; }
        public string eventName { get; set; }
        public string boneName { get; set; }
        public AnimEventType eventType { get; set; }
        public string side { get; set; }
        public string phase { get; set; }
        public string customEvent { get; set; }
        public float value { get; set; }
        
        public Anim_Event()
        {

        }
        public Anim_Event(animAnimEvent_FootPlant fp, string pAnim = "")
        {
            parentAnim = pAnim;
            eventType = AnimEventType.FootPlant;
            if (fp != null)
            {
                customEvent = fp.CustomEvent != null ? fp.CustomEvent.Value : "";
                durationInFrames = fp.DurationInFrames != null ? (int)fp.DurationInFrames.Value : -1;
                startFrame = fp.StartFrame != null ? (int)fp.StartFrame.Value : -1;
                eventName = fp.EventName != null ? fp.EventName.Value : "";
                if (fp.Side != null)
                {
                    side = fp.Side.Value.ToString();
                }
            }
        }
        public Anim_Event(animAnimEvent_FootPhase fp, string pAnim = "")
        {
            parentAnim = pAnim;
            eventType = AnimEventType.FootPhase;
            if (fp != null)
            {
                if (fp.Phase != null)
                {
                    phase = fp.Phase.Value.ToString();
                }
                durationInFrames = fp.DurationInFrames != null ? (int)fp.DurationInFrames.Value : -1;
                startFrame = fp.StartFrame != null ? (int)fp.StartFrame.Value : -1;
                eventName = fp.EventName != null ? fp.EventName.Value : "";
            }
        }
        public Anim_Event(animAnimEvent_Valued ev, string pAnim = "")
        {
            parentAnim = pAnim;
            eventType = AnimEventType.Valued;
            if (ev != null)
            {
                eventName = ev.EventName != null ? ev.EventName.Value : "";
                durationInFrames = ev.DurationInFrames != null ? (int)ev.DurationInFrames.Value : -1;
                startFrame = ev.StartFrame != null ? (int)ev.StartFrame.Value : -1;
                value = ev.Value != null ? ev.Value.Value : 0.0f;
            }
        }
        public Anim_Event(animAnimEvent_SceneItem ev, string pAnim = "")
        {
            parentAnim = pAnim;
            eventType = AnimEventType.SceneItem;
            if (ev != null)
            {
                eventName = ev.EventName != null ? ev.EventName.Value : "";
                durationInFrames = ev.DurationInFrames != null ? (int)ev.DurationInFrames.Value : -1;
                startFrame = ev.StartFrame != null ? (int)ev.StartFrame.Value : -1;
                boneName = ev.BoneName != null ? ev.BoneName.Value : "";
            }
        }
        public Anim_Event(animAnimEvent_WorkspotItem ws, string pAnim = "")
        {
            var actionList = new List<object>();
            var actions = ws.Actions;
            foreach(var a in actions)
            {
                var wsia = a.GetReference().Data;
                switch (wsia.REDType)
                {
                    case "workEquipInventoryWeaponAction":
                        actionList.Add(new AnimAction_EquipInventoryWeapon(wsia as workEquipInventoryWeaponAction));
                        //var mt = WolvenKit.Modkit.RED4.MemTools._memTools;
                        //mt.LoadResource("");
                        break;
                    case "workEquipItemToSlotAction":

                        break;
                    case "workEquipPropToSlotAction":
                        
                        break;
                    case "workUnequipFromSlotAction":
                        break;
                    case "workUnequipItemAction":
                        break;
                    case "workUnequipPropAction":
                        break;
                    default:
                        break;
                }
            }
        }
    }

}
