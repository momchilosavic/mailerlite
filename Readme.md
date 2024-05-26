# Description
### Environment preparation (Only for code development purpose)
To build go application and use operator-sdk, docker container was used:
* operator/golang/Dockerfile
* operator/golang/docker-compose.yaml
* operator-sdk_linux_amd64

### Build and deploy operator
When code is ready, `operator/golang/src/Dockerfile` is used to build operator image: `docker build ./operator/golang/src -t email-operator:golang`
</br>For testing purpose, minikube and local image were used. Because of that, deployment definition in manifests/operator-deployment.yaml defines NeverPullPolicy and email-operator:golang image for cotainer image
</br>Based on your environment `docker build` command and manifest must be adapted.
</br>When image is ready, deploy Kubernetes resources:
* crds
* RBAC manifests
* deployment
</br>When everything is ready, use `manifests/example.yaml` to test the operator

# Project structure
* manifests - CRDs definitions, deployment, service account and RBAC manifests including example manifest
* operator
  * golang
    * src
      * api
        * v1
          * email_types.go
          * emailsenderconfig_types.go
      * internal
        * controller
          * email_controller.yaml
          * emailsenderconfig_controller.yaml
    * docker-compose.yaml
    * Dockerfile
    * operator-sdk_linux_amd64
* screenshoots

# Tests
### Successful email
##### Controller
![Screenshot](/screenshots/email-controller-success.png)
##### Email resource
![Screenshot](/screenshots/email-success.png)
##### MailerSend GUI
![Screenshot](/screenshots/mailersend-activity.png)
![Screenshot](/screenshots/mailersend-domain.png)
### Sender Configuration failure
##### Controller
![Screenshot](/screenshots/email-controller-failure-senderconfig.png)
##### Email resource
![Screenshot](/screenshots/email-failure-senderconfig.png)
### Secret failure
##### Controller
![Screenshot](/screenshots/email-controller-failure-secret.png)
##### Email resource
![Screenshot](/screenshots/email-failure-secret.png)
### HTTP Request failure
##### Controller
![Screenshot](/screenshots/email-controller-failure-request.png)
##### Email resource
![Screenshot](/screenshots/email-failure-request.png)
