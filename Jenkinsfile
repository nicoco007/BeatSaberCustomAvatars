pipeline {
  agent {
    node {
      label "windows && vs-19 && beat-saber"
    }
  }
  environment {
    GIT_HASH = """${bat(
                  returnStdout: true,
                  script: "@git log -n 1 --pretty=%%h"
               )}""".trim()
    
    GIT_REV = """${bat(
                  returnStdout: true,
                  script: "@git tag -l --points-at HEAD"
               )}""".split("\n").last().trim()
    
    GIT_VERSION = """${GIT_REV.length() > 0 ? GIT_REV : GIT_HASH}"""
  }
  stages {
    stage("Prepare") {
      steps {
        bat 'python bsipa_version_hash.py "Source\\CustomAvatar\\manifest.json" "Source\\CustomAvatar\\Properties\\AssemblyInfo.cs"'
      }
    }
    stage("Build Debug") {
      steps {        
        bat "dotnet build Source\\CustomAvatar\\CustomAvatar.csproj -c Debug -p:AutomatedBuild=true"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-DEBUG.zip -r \"./Source/CustomAvatar/bin/Debug/netstandard2.0/*\""

        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-DEBUG.zip"
      }
    }
    stage("Build Release") {
      steps {
        bat "dotnet build Source\\CustomAvatar\\CustomAvatar.csproj -c Release -p:AutomatedBuild=true"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-RELEASE.zip -r \"./Source/CustomAvatar/bin/Release/netstandard2.0/*\""
        
        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-RELEASE.zip"
      }
    }
    stage("Build Editor") {
      steps {
        bat "dotnet build Source\\CustomAvatar-Editor\\CustomAvatar-Editor.csproj -c Release"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-Editor.zip -r \"./Source/CustomAvatar-Editor/bin/Release/netstandard2.0/*\""

        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-Editor.zip"
      }
    }
  }
}
