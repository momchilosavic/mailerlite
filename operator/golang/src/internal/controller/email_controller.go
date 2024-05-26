/*
Copyright 2024.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package controller

import (
	"context"
	"encoding/json"
	"net/http"
	"bytes"
	"fmt"
	"io"

	"k8s.io/apimachinery/pkg/runtime"
    "k8s.io/apimachinery/pkg/api/errors"
    corev1 "k8s.io/api/core/v1"
	ctrl "sigs.k8s.io/controller-runtime"
	"sigs.k8s.io/controller-runtime/pkg/client"
	"sigs.k8s.io/controller-runtime/pkg/log"

	mailerlitecomv1 "github.com/momchilosavic/mailerlite/api/v1"
)

// EmailReconciler reconciles a Email object
type EmailReconciler struct {
	client.Client
	Scheme *runtime.Scheme
}

//+kubebuilder:rbac:groups=mailerlite.com,resources=emails,verbs=get;list;watch;create;update;patch;delete
//+kubebuilder:rbac:groups=mailerlite.com,resources=emails/status,verbs=get;update;patch
//+kubebuilder:rbac:groups=mailerlite.com,resources=emails/finalizers,verbs=update
//+kubebuilder:rbac:groups=v1,resources=secrets,verbs=get

// Reconcile is part of the main kubernetes reconciliation loop which aims to
// move the current state of the cluster closer to the desired state.
// TODO(user): Modify the Reconcile function to compare the state specified by
// the Email object against the actual cluster state, and then
// perform operations to make the cluster state reflect the state specified by
// the user.
//
// For more details, check Reconcile and its Result here:
// - https://pkg.go.dev/sigs.k8s.io/controller-runtime@v0.15.0/pkg/reconcile
func (r *EmailReconciler) Reconcile(ctx context.Context, req ctrl.Request) (ctrl.Result, error) {
	logger := log.FromContext(ctx)

	err := error(nil)

	email := &mailerlitecomv1.Email{}
	err = r.Get(ctx, req.NamespacedName, email)
	if err != nil {
		if errors.IsNotFound(err) {
			return ctrl.Result{}, nil
		}
		return ctrl.Result{}, err
	}
	
	// skip if not newly created
	if email.Status.DeliveryStatus != "" {
		return ctrl.Result{}, nil
	}
	
	config := &mailerlitecomv1.EmailSenderConfig{}
	err = r.Get(ctx, client.ObjectKey{Namespace: req.NamespacedName.Namespace, Name: email.Spec.SenderConfigRef}, config)
	if err != nil {
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: "Sender config not found"}
		_ = r.Status().Update(ctx, email)
		return ctrl.Result{}, err
	}
	
	secret := &corev1.Secret{}
	err = r.Get(ctx, client.ObjectKey{Namespace: req.Namespace, Name: config.Spec.ApiTokenSecretRef}, secret)
	if err != nil {
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: "Api Token Secret not found"}
		_ = r.Status().Update(ctx, email)
		return ctrl.Result{}, err
	}
	
	apiToken := string(secret.Data["token"])
	url := "https://api.mailersend.com/v1/email"
	
	requestBody, err := json.Marshal(map[string]interface{} {
		"from": map[string]string {
			"email": config.Spec.SenderEmail,
		},
		"to": []map[string]string {
			{ "email": email.Spec.RecipientEmail },
		},
		"subject": email.Spec.Subject,
		"text": email.Spec.Body,
	})
	if err != nil {
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: fmt.Sprintf("%s", err)}
		return ctrl.Result{}, err
	}
	
	httpReq, err := http.NewRequest("POST", url, bytes.NewBuffer(requestBody))
	if err != nil {
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: fmt.Sprintf("%s", err)}
		return ctrl.Result{}, err
	}
	httpReq.Header.Set("Content-Type", "application/json")
	httpReq.Header.Set("Authorization", fmt.Sprintf("Bearer %s", apiToken))
	
	client := &http.Client{}
	
	httpResp, err := client.Do(httpReq)
	if err != nil {
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: fmt.Sprintf("%s", err)}
		return ctrl.Result{}, err
	}

	defer httpResp.Body.Close()

	if httpResp.StatusCode < 200 && httpResp.StatusCode >= 300 {
		bodyBytes, err := io.ReadAll(httpResp.Body)
    		if err != nil {
			email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: fmt.Sprintf("%s", err)}
			return ctrl.Result{}, err
    		}
    		bodyString := string(bodyBytes)
    		logger.Info(fmt.Sprintf("%i: %s", httpResp.StatusCode, bodyString))
		email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Failed", MessageId: "", Error: fmt.Sprintf("%s", err)}
		return ctrl.Result{}, fmt.Errorf("%i: %s", httpResp.StatusCode, bodyString)
	}

	logger.Info("Email sent")
	email.Status = mailerlitecomv1.EmailStatus{DeliveryStatus: "Succeeded", MessageId: "", Error: ""}
	return ctrl.Result{}, nil
}

// SetupWithManager sets up the controller with the Manager.
func (r *EmailReconciler) SetupWithManager(mgr ctrl.Manager) error {
	return ctrl.NewControllerManagedBy(mgr).
		For(&mailerlitecomv1.Email{}).
		Complete(r)
}
