namespace WolvenKit.RED4.TweakDB.Types
{
    public partial class gamedataAISubActionSetEquipPrimaryWeapons_Record : gamedataTweakDBRecord
    {
        [RED("animationTime")]
        public CFloat AnimationTime
        {
            get => GetProperty<CFloat>();
            set => SetProperty<CFloat>(value);
        }
    }
}