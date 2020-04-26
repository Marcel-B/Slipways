using System;
namespace com.b_velop.Slipways.Application.Slipway
{
    public class SlipwayDto
    {    
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string Water { get; set; }
    }
}
