using SQLite;

namespace AEPOS.PriceCheck.SQLiteClasses.Models
{
    public class StoreInfo
    {
        [PrimaryKey]
        [Column("storeguid")]
        public string StoreGUID { get; set; }

        [Column("connectionstring")]
        public string ConnectionString { get; set; }

        [Column("storeid")]
        public string StoreID { get; set; }

        [Column("storename")]
        public string StoreName { get; set; }

        [Column("city")]
        public string City { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("stdateformat")]
        public string STDateFormat { get; set; }

    }


}
