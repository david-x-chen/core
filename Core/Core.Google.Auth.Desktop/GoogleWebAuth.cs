﻿using System;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Core.Calendar.Google;
using System.IO;

namespace Core.Google.Auth.Desktop
{
	public class GoogleWebAuth : IGoogleAuth
	{
		internal readonly string ClientId;
		internal readonly string ClientSecret;

		public GoogleWebAuth (string clientId, string clientSecret)
		{
			ClientId = clientId;
			ClientSecret = clientSecret;
		}

		public UserCredential Authorize (string googleUser)
		{
			string appName = Path.GetFileNameWithoutExtension (System.Reflection.Assembly.GetEntryAssembly ().Location);
			return GoogleWebAuthorizationBroker.AuthorizeAsync (
				new ClientSecrets {
					ClientId = ClientId,
					ClientSecret = ClientSecret
				},
				new[] { "https://www.googleapis.com/auth/calendar" },
				googleUser, System.Threading.CancellationToken.None, 
				new FileDataStore (appName)
			).Result;
		}
	}
}

