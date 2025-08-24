using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ModBusDevExpress.Models
{
    [Table("ModBusData")]
    public class AcquiredData
    {
        [Key]
        public Guid ID { get; set; }
        
        public Guid? CheckCompanyObjectID { get; set; } // GUID 타입 유지
        
        public string FacilityCode { get; set; }
        
        public double NumericData { get; set; }
        
        public string StringData { get; set; }
        
        public string IPAddress { get; set; }
        
        public Guid? CompanyObjectID { get; set; } // GUID 타입 유지
        
        public Guid? FactoryObjectID { get; set; }
        
        public DateTime CreateDateTime { get; set; }
        
        public string CreateUserId { get; set; } // 회사명 저장용으로 재활용
        
        public DateTime ModifiedDate { get; set; }

        public AcquiredData()
        {
            ID = Guid.NewGuid();
            CreateDateTime = DateTime.Now;
            ModifiedDate = DateTime.Now;
        }
    }
}