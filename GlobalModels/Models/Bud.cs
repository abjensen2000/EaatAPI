namespace EaatAPI.Models
{
    public class Bud
    {
        public Bud() { }

        public Bud(int? bestillingId = null)
        {
            BestillingId = bestillingId;
        }
        public int Id { get; set; }
        public int? BestillingId { get; set; }


    }
}