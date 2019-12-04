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
        bat 'msbuild Source\\CustomAvatar\\CustomAvatar.csproj /p:Configuration=Debug /p:Platform=AnyCPU /p:AutomatedBuild=true'
        bat 'copy Source\\CustomAvatar\\bin\\Debug\\CustomAvatar.dll Packaging-Debug\\Plugins'
        bat 'copy Source\\CustomAvatar\\bin\\Debug\\CustomAvatar.pdb Packaging-Debug\\Plugins'
        bat '7z a BeatSaberCustomAvatars.DEBUG.zip -r "./Packaging-Debug/*"'
        archiveArtifacts 'BeatSaberCustomAvatars.DEBUG.zip'
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
        bat 'msbuild Source\\CustomAvatar\\CustomAvatar.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:AutomatedBuild=true'
        bat 'copy Source\\CustomAvatar\\bin\\Release\\CustomAvatar.dll Packaging-Release\\Plugins'
        bat '7z a BeatSaberCustomAvatars.RELEASE.zip -r "./Packaging-Release/*"'
        archiveArtifacts 'BeatSaberCustomAvatars.RELEASE.zip'
      }
    }
    stage('Prepare Editor') {
      steps {
        bat 'mkdir Packaging-Editor'
      }
    }
    stage('Build Editor') {
      steps {
        bat 'msbuild Source\\CustomAvatar-Editor\\CustomAvatar-Editor.csproj /p:Configuration=Release /p:Platform=AnyCPU /p:AutomatedBuild=true'
        bat 'copy Source\\CustomAvatar-Editor\\bin\\Release\\CustomAvatar.dll Packaging-Editor'
        bat '7z a BeatSaberCustomAvatars-Editor.zip -r "./Packaging-Editor/*"'
        archiveArtifacts 'BeatSaberCustomAvatars-Editor.zip'
      }
    }
    stage('Clean up') {
      steps {
        cleanWs(cleanWhenAborted: true, cleanWhenFailure: true, cleanWhenNotBuilt: true, cleanWhenSuccess: true, cleanWhenUnstable: true, deleteDirs: true)
      }
    }
  }
}
