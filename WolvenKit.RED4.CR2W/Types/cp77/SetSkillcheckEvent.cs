using WolvenKit.RED4.CR2W.Reflection;
using FastMember;
using static WolvenKit.RED4.CR2W.Types.Enums;

namespace WolvenKit.RED4.CR2W.Types
{
	[REDMeta]
	public class SetSkillcheckEvent : redEvent
	{
		private CHandle<BaseSkillCheckContainer> _skillcheckContainer;

		[Ordinal(0)] 
		[RED("skillcheckContainer")] 
		public CHandle<BaseSkillCheckContainer> SkillcheckContainer
		{
			get => GetProperty(ref _skillcheckContainer);
			set => SetProperty(ref _skillcheckContainer, value);
		}

		public SetSkillcheckEvent(IRed4EngineFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }
	}
}
