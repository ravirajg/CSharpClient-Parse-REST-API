using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parse
{
    public class GeoPoint
    {
        public string __type { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }

        public GeoPoint() { __type = "GeoPoint"; }
        public GeoPoint(double _latitude, double _longitude)
        {
            __type = "GeoPoint";
            latitude = _latitude;
            longitude = _longitude;
        }
    }
    
}
