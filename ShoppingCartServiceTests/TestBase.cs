using AutoMapper;
using ShoppingCartService.Mapping;

namespace ShoppingCartServiceTests
{
    // Put common infrastructure in here
    // Never, ever put test logic or initialize tested classes in this class
    public abstract class TestBase
    {
        protected TestBase()
        {
            // Ideally do not write any test related logic here
            // Only infrastructure and environment setup

            var config = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile()));

            Mapper = config.CreateMapper();
        }

        protected IMapper Mapper { get; }
    }
}