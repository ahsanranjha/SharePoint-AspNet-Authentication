﻿using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Owin.SharePoint.Addin.Authentication.Common;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security.Cookies;

namespace SPAddinOwin.Sample.UrlPath.Common
{
	public class AdddInCookieAuthenticationProvider : CookieAuthenticationProvider
	{
		public override Task ValidateIdentity(CookieValidateIdentityContext context)
		{
			if (context.Identity.IsAuthenticated)
			{
				var shortHandUrl = context.Request.Path.ToString()
					.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries)
					.ToList()
					.FirstOrDefault();

				if (shortHandUrl != null && (shortHandUrl.Equals("auth", StringComparison.OrdinalIgnoreCase) ||
					shortHandUrl.StartsWith("signin") || context.Request.Path.Value.Contains(context.Options.LoginPath.Value)))
				{
					return Task.FromResult<object>(null);
				}

				var shortHandUrlClaim = context.Identity.FindFirst(SPAddinClaimTypes.ShortHandUrl).Value;

				if (!shortHandUrlClaim.Equals(shortHandUrl, StringComparison.OrdinalIgnoreCase))
				{
					context.RejectIdentity();
				}
			}
			return Task.FromResult<object>(null);
		}

		public override void ResponseSignIn(CookieResponseSignInContext context)
		{
			var hostUrl = context.Identity.FindFirst(SPAddinClaimTypes.SPHostUrl).Value;

			var navigationManager = new NavigationManager(context.OwinContext);

			var host = navigationManager.EnsureHostByUrl(hostUrl);

			context.Identity.AddClaim(new Claim(SPAddinClaimTypes.ShortHandUrl, host.ShortHandUrl));
		}

		public override void ApplyRedirect(CookieApplyRedirectContext context)
		{
			var shortHandUrl = context.Request.Path.ToString()
							.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries)
							.ToList()
							.FirstOrDefault();

			if (shortHandUrl == null)
			{
				throw new Exception("Unable to determine host url");
			}
			var queryString = new Uri(context.RedirectUri).ParseQueryString();
			var returnUrl = queryString[context.Options.ReturnUrlParameter];
			var authRedirect = new UriBuilder(context.Request.Uri.GetLeftPart(UriPartial.Path))
			{
				Path = shortHandUrl + context.Options.LoginPath
			};
			var redirectUrl = WebUtilities.AddQueryString(authRedirect.ToString(), context.Options.ReturnUrlParameter, returnUrl);

			context.Response.Redirect(redirectUrl);
		}
	}
}