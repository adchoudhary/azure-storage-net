﻿//-----------------------------------------------------------------------
// <copyright file="SharedKeyLiteCanonicalizer.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.Core.Auth
{
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// Represents a canonicalizer that converts HTTP request data into a standard form appropriate for signing via 
    /// the Shared Key Lite authentication scheme for the Blob or Queue service.
    /// </summary>
    /// <seealso href="http://msdn.microsoft.com/en-us/library/Azure/dd179428.aspx">Authentication for the Microsoft Azure Storage Services</seealso>
    public sealed class SharedKeyLiteCanonicalizer : ICanonicalizer
    {
        private const string SharedKeyLiteAuthorizationScheme = "SharedKeyLite";
        private const int ExpectedCanonicalizedStringLength = 250;

        private static SharedKeyLiteCanonicalizer instance = new SharedKeyLiteCanonicalizer();

        /// <summary>
        /// Gets a static instance of the <see cref="SharedKeyLiteCanonicalizer"/> object.
        /// </summary>
        /// <value>The static instance of the class.</value>
        /// <seealso href="http://msdn.microsoft.com/en-us/library/Azure/dd179428.aspx">Authentication for the Microsoft Azure Storage Services</seealso>
        public static SharedKeyLiteCanonicalizer Instance
        {
            get
            {
                return SharedKeyLiteCanonicalizer.instance;
            }
        }

        private SharedKeyLiteCanonicalizer()
        {
        }

        /// <summary>
        /// Gets the authorization scheme used for canonicalization.
        /// </summary>
        /// <value>The authorization scheme used for canonicalization.</value>
        /// <seealso href="http://msdn.microsoft.com/en-us/library/Azure/dd179428.aspx">Authentication for the Microsoft Azure Storage Services</seealso>
        public string AuthorizationScheme
        {
            get
            {
                return SharedKeyLiteAuthorizationScheme;
            }
        }

        /// <summary>
        /// Converts the specified HTTP request data into a standard form appropriate for signing.
        /// </summary>
        /// <param name="request">The HTTP request that needs to be signed.</param>
        /// <param name="accountName">The name of the storage account that the HTTP request will access.</param>
        /// <returns>The canonicalized string containing the HTTP request data in a standard form appropriate for signing.</returns>
        /// <seealso href="http://msdn.microsoft.com/en-us/library/Azure/dd179428.aspx">Authentication for the Microsoft Azure Storage Services</seealso>
        public string CanonicalizeHttpRequest(HttpRequestMessage request, string accountName)
        {
            CommonUtility.AssertNotNull("request", request);

            // Add the method (GET, POST, PUT, or HEAD).
            CanonicalizedString canonicalizedString = new CanonicalizedString(request.Method.Method, ExpectedCanonicalizedStringLength);

            // Add the Content-* HTTP headers. Empty values are allowed.
            if (request.Content != null)
            {
                canonicalizedString.AppendCanonicalizedElement((request.Content.Headers.ContentMD5 == null) ? null :
                    Convert.ToBase64String(request.Content.Headers.ContentMD5));
                //canonicalizedString.AppendCanonicalizedElement(HttpRequestParsers.GetContentCRC64(request)); // should be covered by custom headers
                canonicalizedString.AppendCanonicalizedElement((request.Content.Headers.ContentType == null) ? null :
                    request.Content.Headers.ContentType.ToString());
            }
            else
            {
                canonicalizedString.AppendCanonicalizedElement(null);
                canonicalizedString.AppendCanonicalizedElement(null);
            }

            // Add the Date HTTP header (only if the x-ms-date header is not being used)
            AuthenticationUtility.AppendCanonicalizedDateHeader(canonicalizedString, request);

            // Add any custom headers
            AuthenticationUtility.AppendCanonicalizedCustomHeaders(canonicalizedString, request);

            // Add the canonicalized URI element
            string resourceString = AuthenticationUtility.GetCanonicalizedResourceString(request.RequestUri, accountName, true);
            canonicalizedString.AppendCanonicalizedElement(resourceString);

            return canonicalizedString.ToString();
        }
    }
}
