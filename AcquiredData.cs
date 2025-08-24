using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModBusDevExpress.Models
{
    public class AcquiredData : XPLiteObject
    {
        [Key(true)]
        public Guid Oid
        {
            get { return GetPropertyValue<Guid>(nameof(Oid)); }
            set { SetPropertyValue(nameof(Oid), value); }
        }
        public string FacilityCode
        {
            get { return GetPropertyValue<string>(nameof(FacilityCode)); }
            set { SetPropertyValue(nameof(FacilityCode), value); }
        }
        public double NumericData
        {
            get { return GetPropertyValue<double>(nameof(NumericData)); }
            set { SetPropertyValue(nameof(NumericData), value); }
        }
        public string StringData
        {
            get { return GetPropertyValue<string>(nameof(StringData)); }
            set { SetPropertyValue(nameof(StringData), value); }
        }
        public string IPAddres
        {
            get { return GetPropertyValue<string>(nameof(IPAddres)); }
            set { SetPropertyValue(nameof(IPAddres), value); }
        }
        public DateTime CreatedDateTime
        {
            get { return GetPropertyValue<DateTime>(nameof(CreatedDateTime)); }
            set { SetPropertyValue(nameof(CreatedDateTime), value); }
        }

        #region Constructors
        public AcquiredData(Session session) : base(session) { }
        #endregion

        #region Methods
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place your initialization code here (https://documentation.devexpress.com/eXpressAppFramework/CustomDocument112834.aspx).

            CreatedDateTime = DateTime.Now;
        }
        #endregion
    }
}