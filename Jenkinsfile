pipeline {
  agent {
    node {
      label "windows && vs-15"
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
    stage("Prepare Debug") {
      steps {
        bat "robocopy Packaging Packaging-Debug /E & if %ERRORLEVEL% LEQ 3 (exit /b 0)"
        bat "mkdir Packaging-Debug\\Plugins"
      }
    }
    stage("Build Debug") {
      steps {
        bat "msbuild Source\\CustomAvatar\\CustomAvatar.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:AutomatedBuild=true"
        bat "copy Source\\CustomAvatar\\bin\\Debug\\CustomAvatar.dll Packaging-Debug\\Plugins"
        bat "copy Source\\CustomAvatar\\bin\\Debug\\CustomAvatar.pdb Packaging-Debug\\Plugins"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-DEBUG.zip -r \"./Packaging-Debug/*\""
        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-DEBUG.zip"
      }
    }
    stage("Prepare Release") {
      steps {
        bat "robocopy Packaging Packaging-Release /E & if %ERRORLEVEL% LEQ 3 (exit /b 0)"
        bat "mkdir Packaging-Release\\Plugins"
      }
    }
    stage("Build Release") {
      steps {
        bat "msbuild Source\\CustomAvatar\\CustomAvatar.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:AutomatedBuild=true"
        bat "copy Source\\CustomAvatar\\bin\\Release\\CustomAvatar.dll Packaging-Release\\Plugins"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-RELEASE.zip -r \"./Packaging-Release/*\""
        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-RELEASE.zip"
      }
    }
    stage("Prepare Editor") {
      steps {
        bat "mkdir Packaging-Editor"
      }
    }
    stage("Build Editor") {
      steps {
        bat "msbuild Source\\CustomAvatar-Editor\\CustomAvatar-Editor.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:AutomatedBuild=true"
        bat "copy Source\\CustomAvatar-Editor\\bin\\Release\\CustomAvatar.dll Packaging-Editor"
        bat "copy Libraries\\FinalIK.dll Packaging-Editor"
        bat "copy Libraries\\DynamicBone.dll Packaging-Editor"
        bat "7z a BeatSaberCustomAvatars-${env.GIT_VERSION}-Editor.zip -r \"./Packaging-Editor/*\""
        archiveArtifacts "BeatSaberCustomAvatars-${env.GIT_VERSION}-Editor.zip"
      }
    }
    stage("Clean up") {
      steps {
        cleanWs(cleanWhenAborted: true, cleanWhenFailure: true, cleanWhenNotBuilt: true, cleanWhenSuccess: true, cleanWhenUnstable: true, deleteDirs: true)
      }
    }
  }
}
