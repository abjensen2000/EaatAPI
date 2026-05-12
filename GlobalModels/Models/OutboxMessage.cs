using System;
using System.Collections.Generic;
using System.Text;

namespace Global.Models
{
    public class OutboxMessage
    {
        private int _id;
        private DateTime _oprettetDato;
        private string _exchangeName;
        private string _routingKey;
        private string _payload;
        private bool _erSendt;
        private string? FejlBesked;

        public OutboxMessage(string exchangeName, string routingKey, string payload)
        {
            _id = 0;
            _oprettetDato = DateTime.UtcNow;
            _exchangeName = exchangeName;
            _routingKey = routingKey;
            _payload = payload;
            _erSendt = false;
        }

        public int Id { get => _id; set => _id = value; }
        public DateTime OprettetDato { get => _oprettetDato; set => _oprettetDato = value; }
        public string ExchangeName { get => _exchangeName; set => _exchangeName = value; }
        public string RoutingKey { get => _routingKey; set => _routingKey = value; }
        public string Payload { get => _payload; set => _payload = value; }
        public bool ErSendt { get => _erSendt; set => _erSendt = value; }
        public string? FejlBesked1 { get => FejlBesked; set => FejlBesked = value; }
    }
}
