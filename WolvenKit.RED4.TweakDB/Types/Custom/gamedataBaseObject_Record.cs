namespace WolvenKit.RED4.TweakDB.Types
{
    public partial class gamedataBaseObject_Record : gamedataTweakDBRecord
    {
        [RED("weakspots")]
        public CArray<TweakDBID> Weakspots
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
        
        [RED("statPools")]
        public CArray<TweakDBID> StatPools
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
        
        [RED("statModifiers")]
        public CArray<TweakDBID> StatModifiers
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
        
        [RED("effectors")]
        public CArray<TweakDBID> Effectors
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
        
        [RED("savable")]
        public CBool Savable
        {
            get => GetProperty<CBool>();
            set => SetProperty<CBool>(value);
        }
        
        [RED("statModifierGroups")]
        public CArray<TweakDBID> StatModifierGroups
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
        
        [RED("objectActions")]
        public CArray<TweakDBID> ObjectActions
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
    }
}