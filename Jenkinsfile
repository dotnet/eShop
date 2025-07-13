pipeline {
    agent any

    stages {
        stage('Checkout') {
            steps {
                git branch: 'dev', url: 'https://github.com/ealinour/eShop-pfe.git'
            }
        }

        stage('Build') {
            steps {
                echo 'Build step here (ex: docker build, dotnet build...)'
            }
        }

        stage('Test') {
            steps {
                echo 'Run tests here'
            }
        }

        stage('Deploy') {
            steps {
                echo 'Deploy to Kubernetes / Docker / etc'
            }
        }
    }
}
