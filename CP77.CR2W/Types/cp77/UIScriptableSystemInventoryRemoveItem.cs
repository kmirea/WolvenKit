using CP77.CR2W.Reflection;
using FastMember;
using static CP77.CR2W.Types.Enums;

namespace CP77.CR2W.Types
{
	[REDMeta]
	public class UIScriptableSystemInventoryRemoveItem : gameScriptableSystemRequest
	{
		[Ordinal(0)] [RED("itemID")] public gameItemID ItemID { get; set; }

		public UIScriptableSystemInventoryRemoveItem(CR2WFile cr2w, CVariable parent, string name) : base(cr2w, parent, name) { }
	}
}