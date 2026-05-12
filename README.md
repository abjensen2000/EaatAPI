EaatApi
Systemet er bygget op af 3 klienter: Restaurant, Kunde, og Bud. Disse kan alle 3 interagere med API’et: EaatAPI, gennem gets, puts, og posts osv. De er også alle 3 abonneret til API’et gennem forskellige exchanges. 
Kunde er abonneret til ”notifikationTilKunde”-Exchange, der får en opdatering hver gang; restaurant accepterer bestilling, og når bud accepterer bestilling. RoutingKey er her sat til KundeId. Den bliver kaldt både i:
       [HttpPut("bestillinger/{bestillingId}/accepterBud/{budId}")]
og    
       [HttpPut("bestillinger/{id}/accepterRestaurant")].
       
Restaurant er abonneret til ”bestillingerFraAPITilRestaurant”, som bliver kaldt når en kunde post’er en bestilling til API’et:
        [HttpPost("bestillinger")].

Bud abonnerer til ”bestillingerFraRestaurantTilBud”, som bliver kaldt når restaurant accepterer en bestilling gennem:
        [HttpPut("bestillinger/{id}/accepterRestaurant")].
        
Alle exchanged er sat til direct, udover API til Bud, da det er alle bud der skal have alle bestillinger. Bestilling til restaurant, og notifikation til kunde, har begge RoutingKey med reference til deres eget ID.
Queue er sat til durable og har et dedikeret kønavn fra API til Kunde, så kunden kan lukke klienten ned efter at have bestilt, og hvis restaurant eller bud har accepteret i mellemtiden, så vil de få en notifikation næste gang de starter klienten.  
