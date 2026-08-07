using ECommerce.API.GraphQL.Mutations;
using ECommerce.API.GraphQL.Queries;
using ECommerce.API.GraphQL.Types;

public static class GraphQLExtension
{
  public static IServiceCollection AddGraphQLDependencies(this IServiceCollection services)
  {
    services
      .AddGraphQLServer()
      .AddQueryType<Query>()
      .AddMutationType<Mutation>()
      .AddType<UserType>()
      .AddAuthorization()
      .AddProjections()
      .AddFiltering()
      .AddSorting()
      .AddErrorFilter<AuthorizationErrorFilter>()
      .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = false);

    return services;
  }
}