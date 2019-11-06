pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }

  }
  stages {
    stage('Build Debug') {
      steps {
        bat 'msbuild /p:Configuration=Debug /p:Platform="Any CPU"'
        archiveArtifacts CustomAvatar\bin\Debug\CustomAvatar.dll'
        archiveArtifacts CustomAvatar-Editor\bin\Debug\CustomAvatar.dll'
      }
    },
    stage('Build Release') {
      steps {
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU"'
        archiveArtifacts CustomAvatar\bin\Release\CustomAvatar.dll'
        archiveArtifacts CustomAvatar-Editor\bin\Release\CustomAvatar.dll'
      }
    }
  }
}
