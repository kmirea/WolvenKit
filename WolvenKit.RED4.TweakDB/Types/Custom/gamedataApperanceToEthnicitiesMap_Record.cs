namespace WolvenKit.RED4.TweakDB.Types
{
    public partial class gamedataApperanceToEthnicitiesMap_Record : gamedataTweakDBRecord
    {
        [RED("map")]
        public CArray<TweakDBID> Map
        {
            get => GetProperty<CArray<TweakDBID>>();
            set => SetProperty<CArray<TweakDBID>>(value);
        }
    }
}