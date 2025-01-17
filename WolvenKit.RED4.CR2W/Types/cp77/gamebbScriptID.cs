using WolvenKit.RED4.CR2W.Reflection;
using FastMember;
using static WolvenKit.RED4.CR2W.Types.Enums;

namespace WolvenKit.RED4.CR2W.Types
{
	[REDMeta]
	public class gamebbScriptID : CVariable
	{
		private gamebbID _none;

		[Ordinal(0)] 
		[RED("None")] 
		public gamebbID None
		{
			get => GetProperty(ref _none);
			set => SetProperty(ref _none, value);
		}

		public gamebbScriptID(IRed4EngineFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }
	}
}
