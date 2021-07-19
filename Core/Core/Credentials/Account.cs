﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Credentials
{

  public class Account : IEquatable<Account>
  {
    [JsonIgnore]
    public string id
    {
      get
      {
        if (serverInfo == null || userInfo == null)
          throw new SpeckleException("Incomplete account info: cannot generate id.", level: Sentry.SentryLevel.Error);
        return Speckle.Core.Models.Utilities.hashString(serverInfo.url + userInfo.email);
      }
    }

    public string token { get; set; }

    public string refreshToken { get; set; }

    public bool isDefault { get; set; } = false;

    public ServerInfo serverInfo { get; set; }

    public UserInfo userInfo { get; set; }

    public Account() { }

    /// <summary>
    /// Simply checks important properties are not NullOrEmpty
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
      return !string.IsNullOrEmpty(token) &&
            !string.IsNullOrEmpty(userInfo.id) &&
            !string.IsNullOrEmpty(userInfo.email) &&
            !string.IsNullOrEmpty(userInfo.name) &&
            !string.IsNullOrEmpty(serverInfo.url) &&
            !string.IsNullOrEmpty(serverInfo.name);
    }

    #region public methods
    public async Task<UserInfo> Validate()
    {
      using var httpClient = new HttpClient();

      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(serverInfo.url), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

      var request = new GraphQLRequest
      {
        Query = @" query { user { name email id company } }"
      };

      var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request);

      if (response.Errors != null)
        return null;

      return response.Data.user;
    }

    public bool Equals(Account other)
    {
      return other.userInfo.email == userInfo.email && other.serverInfo.url == serverInfo.url;
    }

    public override string ToString()
    {
      return $"Account ({userInfo.email} | {serverInfo.url})";
    }

    #endregion
  }
}
