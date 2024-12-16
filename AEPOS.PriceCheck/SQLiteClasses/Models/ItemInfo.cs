using SQLite;

namespace AEPOS.PriceCheck.SQLiteClasses.Models
{
    public class ItemInfo
    {

        [Column("sku"), NotNull]
        public int SKU { get; set; }

        [Column("upc"), NotNull]
        public int UPC { get; set; }

        [Column("itemname")]
        public string ITEMNAME { get; set; }


    }
}
