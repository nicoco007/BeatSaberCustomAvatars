pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }
  }
  stages {
    stage('Prepare Debug') {
      steps {
        bat 'robocopy Packaging Packaging-Debug /E & if %ERRORLEVEL% LEQ 3 (exit /b 0)'
        bat 'mkdir Packaging-Debug\\Plugins'
      }
    }
    stage('Build Debug') {
      steps {
        bat 'msbuild /p:Configuration=Debug /p:Platform="Any CPU" /p:AutomatedBuild=true'
        bat 'copy CustomAvatar\\bin\\Debug\\CustomAvatar.dll Packaging-Debug\\Plugins'
        bat 'copy CustomAvatar\\bin\\Debug\\CustomAvatar.pdb Packaging-Debug\\Plugins'
        bat '7z a BeatSaber.CustomAvatars.DEBUG.zip -r "./Packaging-Debug/*"'
        archiveArtifacts 'BeatSaber.CustomAvatars.DEBUG.zip'
      }
    }
    stage('Prepare Release') {
      steps {
        bat 'robocopy Packaging Packaging-Release /E & if %ERRORLEVEL% LEQ 3 (exit /b 0)'
        bat 'mkdir Packaging-Release\\Plugins'
      }
    }
    stage('Build Release') {
      steps {
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU" /p:AutomatedBuild=true'
        bat 'copy CustomAvatar\\bin\\Release\\CustomAvatar.dll Packaging-Release\\Plugins'
        bat '7z a BeatSaber.CustomAvatars.RELEASE.zip -r "./Packaging-Release/*"'
        archiveArtifacts 'BeatSaber.CustomAvatars.RELEASE.zip'
      }
    }
    stage('Clean up') {
      steps {
        cleanWs(cleanWhenAborted: true, cleanWhenFailure: true, cleanWhenNotBuilt: true, cleanWhenSuccess: true, cleanWhenUnstable: true, deleteDirs: true)
      }
    }
  }
}
