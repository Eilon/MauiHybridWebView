using System;

namespace HybridWebView
{
    public static class PathUtils
    {
        public const string PlanTextMimeType = "text/plain";
        public const string HtmlMimeType = "text/html";

        public static string NormalizePath(string filename) =>
            filename
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

        public static void GetRelativePathAndContentType(Uri appOriginUri, Uri requestUri, string originalUrl, string? mainFileName, out string? relativePath, out string contentType, out string fullUrl)
        {
            relativePath = appOriginUri.MakeRelativeUri(requestUri).ToString().Replace('/', '\\');
            fullUrl = originalUrl;

            if (string.IsNullOrEmpty(relativePath))
            {
                //The main file may be a URL that has a query string. For example if we want the main page to go through the proxy.
                if (!string.IsNullOrEmpty(mainFileName))
                {
                    relativePath = QueryStringHelper.RemovePossibleQueryString(mainFileName);
                    fullUrl = mainFileName;
                }

                //Try and get the mime type from the full URL (main file could be a URL that has a query string pointing to a file such as a PDF).
                string? minType;
                if (PathUtils.TryGetMimeType(fullUrl, out minType))
                {
                    if (!string.IsNullOrEmpty(minType))
                    {
                        contentType = minType;
                    }
                    else
                    {
                        contentType = PathUtils.HtmlMimeType;
                    }
                }
                else
                {
                    contentType = PathUtils.HtmlMimeType;
                }
            }
            else
            {
                contentType = PathUtils.GetMimeType(fullUrl);
            }
        }

        /// <summary>
        /// Tries to get the mime type from a file name or URL by looking for valid file extensions or mime types in a data URI.
        /// Input can be a file name, url (with query string), a data uri, mime type, or file extension.
        /// </summary>
        /// <param name="fileNameOrUrl">A file name, url (with query string), a data uri, mime type, or file extension.</param>
        /// <returns>The determined mime type. Fallback to "text/plain"</returns>
        public static string GetMimeType(string fileNameOrUrl)
        {
            string? ext;
            string? mimeType;

            //Check for a mime type in a data uri.
            if (fileNameOrUrl.Contains("data:") && fileNameOrUrl.Contains(";base64,"))
            {
                ext = fileNameOrUrl.Substring(5, fileNameOrUrl.IndexOf(";base64,") - 5);

                if (TryGetMimeType(ext, out mimeType))
                {
                    return mimeType ?? PlanTextMimeType;
                }
            }

            //Seperate out query string if it exists.
            string queryString = string.Empty;

            if (fileNameOrUrl.Contains("?"))
            {
                queryString = fileNameOrUrl.Substring(fileNameOrUrl.IndexOf("?"));
                fileNameOrUrl = fileNameOrUrl.Substring(0, fileNameOrUrl.IndexOf("?"));
            }

            //If there is still a url or file name, check it for a valid file extension.
            if (!string.IsNullOrWhiteSpace(fileNameOrUrl))
            {
                ext = Path.GetExtension(fileNameOrUrl);

                if (TryGetMimeType(ext, out mimeType))
                {
                    return mimeType ?? PlanTextMimeType;
                }

                //Try passing the whole file name to see if it is itself a valid mime type. This would work if only a file extension or mimetype was passed in.
                if (TryGetMimeType(fileNameOrUrl, out mimeType))
                {
                    return mimeType ?? PlanTextMimeType;
                }
            }

            //If there is a query string, check it's parameter values to see if it contains something with a valid file extension.
            if (!string.IsNullOrWhiteSpace(queryString))
            {
                var queryParameters = queryString.Split('&');

                foreach (var param in queryParameters)
                {
                    if (param.Contains("="))
                    {
                        ext = param.Substring(param.IndexOf("=") + 1);

                        if (TryGetMimeType(ext, out mimeType))
                        {
                            return mimeType ?? PlanTextMimeType;
                        }
                    }
                }
            }

            //If get here, return plain text mime type.
            return PlanTextMimeType;
        }

        /// <summary>
        /// Looks up a mime type based on a file extension or mime type.
        /// </summary>
        /// <param name="mimeTypeOrFileExtension">A mimeType or file extension to validate and get the mime type for.</param>
        /// <returns>A boolean indicating if it found a supported mime type.</returns>
        public static bool TryGetMimeType(string? mimeTypeOrFileExtension, out string? mimeType)
        {
            mimeType = null;

            if (string.IsNullOrWhiteSpace(mimeTypeOrFileExtension))
            {
                return false;
            }

            //If content type starts with a period, remove it. File extension may have been passed in.
            if (mimeTypeOrFileExtension.StartsWith("."))
            {
                mimeTypeOrFileExtension = mimeTypeOrFileExtension.Substring(1);
            }

            //For simplirity, if the content type contains a slash, assume it is a file path extension.
            if (mimeTypeOrFileExtension.Contains("/"))
            {
                //Return the string after the last index of the slash.
                mimeTypeOrFileExtension = mimeTypeOrFileExtension.Substring(mimeTypeOrFileExtension.LastIndexOf("/") + 1);
            }

            //Sanitize the content type.
            mimeType = mimeTypeOrFileExtension.ToLowerInvariant() switch
            {
                //WebAssembly file types
                "wasm" => "application/wasm",

                //Image file types
                "png" => "image/png",
                "jpg" or "jpeg" or "jfif" or "pjpeg" or "pjp" => "image/jpeg",
                "gif" => "image/gif",
                "webp" => "image/wemp",
                "svg" or "svg+xml" => "image/svg+xml",
                "ico" or "x-icon" => "image/x-icon",
                "bmp" => "image/bmp",
                "tif" or "tiff" => "image/tiff",
                "avif" => "image/avif",
                "apng" => "image/apng",

                //Video file types
                "mp4" => "video/mp4",
                "webm" => "video/webm",
                "mpeg" => "video/mpeg",

                //Audio file types
                "mp3" => "audio/mpeg",
                "wav" => "audio/wav",

                //Font file types
                "woff" => "font/woff",
                "woff2" => "font/woff2",
                "otf" => "font/otf",

                //JSON and XML based file types
                "json" or "geojson" or "geojsonseq" or "topojson" => "application/json",
                "gpx" or "georss" or "gml" or "citygml" or "czml" or "xml" => "application/xml",
                "kml" or "kml+xml" or "vnd.google-earth.kml+xml" => "application/vnd.google-earth.kml+xml",

                //Office file types
                "doc" or "docx" or "msword" => "application/msword",
                "xls" or "xlsx" or "vnd.ms-excel" => "application/vnd.ms-excel",
                "ppt" or "pptx" or "vnd.ms-powerpoint" => "application/vnd.ms-powerpoint",

                //3D model file types commonly used in web.
                "gltf" or "gltf+json" => "model/gltf+json",
                "glb" or "gltf-binary" => "model/gltf-binary",
                "dae" => "model/vnd.collada+xml",

                //Other binary file types
                "zip" => "application/zip",
                "pbf" or "x-protobuf" => "application/x-protobuf",
                "kmz" or "vnd.google-earth.kmz" or "shp" or "dbf" or "bin" or "b3dm" or "i3dm" or "pnts" or "subtree" or "octet-stream" => "application/octet-stream",
                "pdf" => "application/pdf",

                //Other map tile file types
                "terrian" => "application/vnd.quantized-mesh",
                "pmtiles" => "application/vnd.pmtiles",

                //Text based file types
                "htm" or "html" => "text/html",
                "xhtml" => "application/xhtml+xml",
                "js" or "javascript" => "text/javascript",
                "css" => "text/css",
                "csv" => "text/csv",
                "md" => "text/markdown",
                "plain" or "txt" or "wat" => "text/plain",

                _ => null,
            }; 

            return mimeType != null;
        }
    }
}
