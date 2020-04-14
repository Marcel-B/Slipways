using System;

namespace com.b_velop.Slipways.Web.Infrastructure
{
    public static class Queries
    {
        public static Tuple<string, string> Waters = new Tuple<string, string>("waters",
                          @"query {
                              waters {
                                id
                                longname
                                shortname
                              }
                          }");

        public static Tuple<string, string> Extras = new Tuple<string, string>("extras",
                          @"query {
                              extras {
                                id
                                name
                              }
                          }");

        public static Tuple<string, string> Services = new Tuple<string, string>("services",
                          @"query {
                              services {
                                id
                                name
                                city
                                latitude
                                longitude
                                postalcode
                                phone
                                email
                                url
                                street
                                manufacturers {
                                    id
                                    name
                                }
                              }
                           }");

        public static Tuple<string, string> Manufacturers = new Tuple<string, string>("manufacturers",
                          @"query {
                              manufacturers {
                                id
                                name
                              }
                           }");

        public static Tuple<string, string> Slipways = new Tuple<string, string>("slipways",
                          @"query {
                              slipways {
                                id
                                city
                                name
                                longitude
                                latitude
                                costs
                                street
                                port {
                                  id
                                }
                                postalcode
                                water {
                                  id
                                  longname
                                  shortname
                                }
                                extras { 
                                    id
                                    name
                                }
                              }
                           }");

        public static Tuple<string, string> Ports = new Tuple<string, string>("ports",
                    @"query {
                              ports {
                                id
                                city
                                name
                                longitude
                                latitude
                                street
                                postalcode
                                water {
                                  id
                                  longname
                                  shortname
                                }
                              }
                           }");
    }
}
