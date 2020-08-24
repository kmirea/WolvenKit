using System.IO;
using System.Runtime.Serialization;
using WolvenKit.CR2W.Reflection;
using static WolvenKit.CR2W.Types.Enums;


namespace WolvenKit.CR2W.Types
{
	[DataContract(Namespace = "")]
	[REDMeta]
	public class CStoryScenePropEffectEvent : CStorySceneEvent
	{
		[RED("propID")] 		public CName PropID { get; set;}

		[RED("effectName")] 		public CName EffectName { get; set;}

		[RED("startOrStop")] 		public CBool StartOrStop { get; set;}

		public CStoryScenePropEffectEvent(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name){ }

		public static new CVariable Create(CR2WFile cr2w, CVariable parent, string name) => new CStoryScenePropEffectEvent(cr2w, parent, name);

		public override void Read(BinaryReader file, uint size) => base.Read(file, size);

		public override void Write(BinaryWriter file) => base.Write(file);

	}
}