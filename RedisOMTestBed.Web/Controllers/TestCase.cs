namespace RedisOMTestBed.Web.Controllers
{
    public enum TestCase
    {
        // Documents will not expire
        None,
        // Only one will be set to expire almost immediately
        ExpireOne,
        // Half of the documents will be set to expire, evenly distributed in the future
        ExpireHalf,
        // All documents will be set to expire, evenly distributed in the future
        ExpireSoon,
    }
}
