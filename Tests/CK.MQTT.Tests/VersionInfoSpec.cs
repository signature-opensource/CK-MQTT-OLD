/*
   Copyright 2014 NETFX

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0
*/

using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
    using System.IO;
    using System.Text.RegularExpressions;

    public class VersionInfoSpec
    {
        [Test]
        public void when_parsing_version_info_then_can_retrieve_versions()
        {
            string version = @"Assembly=0.1.1
File=0.1.0
Package=0.1.0-pre";

            string assembly = Regex.Match( version, "(?<=Assembly=).*$", RegexOptions.Multiline ).Value.Trim();
            string file = Regex.Match( version, "(?<=File=).*$", RegexOptions.Multiline ).Value.Trim();
            string package = Regex.Match( version, "(?<=Package=).*$", RegexOptions.Multiline ).Value.Trim();

            "0.1.1".Should().Be( assembly );
            "0.1.0".Should().Be( file );
            "0.1.0-pre".Should().Be( package );
        }

        [Test]
        public void when_parsing_version_then_can_read_from_string()
        {
            string Version = "1.0.0-pre";
            string Target = "out.txt";

            string assembly = Version.IndexOf( '-' ) != -1 ?
                Version.Substring( 0, Version.IndexOf( '-' ) ) :
                Version;

            File.WriteAllText( Target,
$@"AssemblyVersion={assembly}, 
FileVersion={assembly},
PackageVersion={Version}" );

        }
    }
}
