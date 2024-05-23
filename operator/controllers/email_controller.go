package controllers

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	
	"github.com/go-logr/logr"
	"email-operator/api/v1"
	corev1 "k8s.io/api/core/v1"
	"k8s.io/apimachinery/pkg/api/errors"
	"k8s.io/apimachinery/pkg/runtime"
	ctrl "sigs.k8s.io/controller-runtime"
	"sigs.k8s.io/controller-runtime/pkg/client"
	"sigs.k8s.io/controller-runtime/pkg/log"
)

type EmailReconciler struct {
	client.Client
	Scheme *runtime.Scheme
	Log logr.Logger
}

func (r *EmailReconciler) Reconcile(ctx context.Context, req ctrl.Request) (ctrl.Result, error) {
	log := r.Log.WithValues("email", req.NamespacedName)
	
	// Email
	email := &emailv1.Email{}
	err := r.Get(ctx, req.NamespacedName, email)
	if err != nil {
		if errors.IsNotFound(err) {
			return ctrl.Result{}, nil
		}
		return ctrl.Result{}, err
	}
	
	// EmailSenderConfig
	senderConfig := &emailv1.EmailSenderConfig{}
	err = r.Get(ctx, client.ObjectKey{Namespace: req.Namespace, Name: email.Spec.SenderConfigRef}, senderConfig)
	if err != nil {
		if errors.IsNotFound(err) {
			email.Status.DeliveryStatus = "Failed"
			email.Status.Error = "Sender config not found"
			r.Status().Update(ctx, email)
			return ctrl,Result{Requeue: true}, nil
		}
		return ctrl.Result{}, err
	}
	
	// API Token
	secret := &corev1.Secret{}
	err = r.Get(ctx, client.ObjectKey{Namespace: req.Namespace, Name:senderConfig.Spec.APITokenSecretRef}, secret)
	if err != nil {
		if errors.IsNotFound(err) {
			email.Status.DeliveryStatus = "Failed"
			email.Status.Error = "API Token not found"
			r.Status().Update(ctx, email)
			return ctrl,Result{Requeue: true}, nil
		}
		return ctrl.Result{}, err
	}
	
	apiToken := string(secret.Data["token"])
	
	// Send email
	deliveryStatus, messageID, err := r.sendEmail(apiToken, senderConfig.Spec.SenderEmail, email.Spec.RecipientEmail, email.Spec.Subject, email.Spec.Body)
	if err != nill {
		email.Status.DeliveryStatus = "Failed"
		email.Status.Error - err.Error()
		r.Status().Update(ctx, email)
		return ctrl.Result{Requeue: true}, nil
	}
	
	email.Status.DeliveryStatus = deliveryStatus
	email.Status.MessageID = messageID
	email.Status.Error = ""
	r.Status().Update(ctx, email)
	
	return ctrl.Result{}, nil
}

func (r *EmailReconciler) sendEmail(apiToken, senderEmai, recipientEmail, subject, body string) (string, string, error) {
	url := "https://api.mailersend.com/v1/email"
	payload := map[string]interface{}{
		"from": map[string]string{
			"email": senderEmail
		},
		"to": []map[string]string{
			"email": recipientEmail
		},
		"subject": subject,
		"text": body
	}
	payloadBytes, err := json.Marshal(payload)
	if err != nil {
		return "Failed", "", err
	}
	
	req, err := http.NewRequest("POST", url, strings.NewReader(string(payloadBytes)))
	if err != nil {
		return "Failed", "", err
	}
	
	req.Header.Add("Content-Type", "application/json")
	req.Header.Add("Authorization", fmt.Sprintf("Bearer %s", apiToken))
	
	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return "Failed", "", err
	}
	defer resp.Body.Close()
	
	if resp.StatusCode != http.StatusOK {
		return "Failed", "", fmt.Errorf("failed to send email: %s", resp,Status)
	}
	
	var respData map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&respData); err != nil {
		return "Failed", "", err
	}
	
	messageID, ok := respData["message_id"].(string)
	if !ok {
		return "Failed", "", fmt.Errorf("failed to parse message ID")
	}
	
	return "Sent", messageID, nil
}

func (r *EmailReconciler) SetupWithManager(mgr ctrl.Manager) error {
	return ctrl.NewControllerManagedBy(mgr).For(&emailv1.Email{}).Complete(r)
}