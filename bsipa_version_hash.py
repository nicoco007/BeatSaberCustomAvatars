import json
import os
import re
import sys

manifest_path, assembly_info_path = sys.argv[1:]

commit_hash = os.getenv("GIT_HASH")
tag = os.getenv("GIT_TAG")

with open(manifest_path, "r") as manifest_file:
    obj = json.load(manifest_file)

semver = re.match('^(?P<prerelease>(?P<version>(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*))(?:-(?:(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?)(?:\+(?:[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$', obj["version"])

if not semver:
    print("Invalid semantic version")
    exit(-1)

numeric_version = semver.group('version')
version_with_prerelease = semver.group('prerelease')

with open(assembly_info_path, "r") as assembly_info_file:
    file_contents = assembly_info_file.read()
    assembly_version = re.search('\[assembly: AssemblyVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.[0-9]+"\)\]', file_contents)
    assembly_file_version = re.search('\[assembly: AssemblyFileVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.[0-9]+"\)\]', file_contents)

    if assembly_version and assembly_version.group(1) != numeric_version:
        print("Mismatched manifest version and assembly version: wanted '{}', got '{}'".format(assembly_version.group(1), numeric_version))
        exit(-1)
    else:
        print("✔ Assembly Version")

    if assembly_file_version and assembly_file_version.group(1) != numeric_version:
        print("Mismatched manifest version and assembly file version: wanted '{}', got '{}'".format(assembly_version.group(1), numeric_version))
        exit(-1)
    else:
        print("✔ Assembly File Version")

if (tag is not None and len(tag) > 0):
    if tag != ("v" + version_with_prerelease):
        print("Git tag does not match manifest version")
        exit(-1)
    else:
        print("✔ Git tag")

    obj["version"] = version_with_prerelease
else:
    if commit_hash is None or len(commit_hash) == 0:
        print("Commit hash not found in environment")
        exit(-1)
    else:
        print("✔ Git hash")

    obj["version"] = version_with_prerelease + "+git." + commit_hash

with open(manifest_path, "w") as manifest_file:
    json.dump(obj, manifest_file, indent=2)