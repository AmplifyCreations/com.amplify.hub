// Amplify Hub
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using System.Collections.Generic;

namespace AmplifyHub
{
	public class AmplifyProduct
	{
		public string Name;
		public string Version;
		public bool Installed;
		public string AssetStoreUrl;
	}

	public static class AmplifyHubRegistry
	{
		private static readonly Dictionary<string, AmplifyProduct> Products = new Dictionary<string, AmplifyProduct>();

		public static void Register( string name, string version, string assetStoreUrl )
		{
			Products[ name ] = new AmplifyProduct
			{
				Name = name,
				Version = version,
				Installed = true,
				AssetStoreUrl = assetStoreUrl
			};
		}

		public static IEnumerable<AmplifyProduct> InstalledProducts()
		{
			return Products.Values;
		}

		public static bool IsInstalled( string name )
		{
			return Products.ContainsKey( name );
		}
	}
}