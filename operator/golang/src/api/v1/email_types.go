/*
Copyright 2024.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0
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

package v1

import (
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
)

// EDIT THIS FILE!  THIS IS SCAFFOLDING FOR YOU TO OWN!
// NOTE: json tags are required.  Any new fields you add must have json tags for the fields to be serialized.

// EmailSpec defines the desired state of Email
type EmailSpec struct {
	// INSERT ADDITIONAL SPEC FIELDS - desired state of cluster
	// Important: Run "make" to regenerate code after modifying this file

	// Foo is an example field of Email. Edit email_types.go to remove/update
	// Foo string `json:"foo,omitempty"`
   	// +kubebuilder:validation:Required
   	// +kubebuilder:validation:XValidation:rule="self == oldSelf",message="Value is immutable"
	SenderConfigRef string `json:"senderConfigRef,omitempty"`
   	// +kubebuilder:validation:Required
   	// +kubebuilder:validation:XValidation:rule="self == oldSelf",message="Value is immutable"
	RecipientEmail string `json:"recipientEmail,omitempty"`
   	// +kubebuilder:validation:Required
   	// +kubebuilder:validation:XValidation:rule="self == oldSelf",message="Value is immutable"
	Subject string `json:"subject,omitempty"`
   	// +kubebuilder:validation:Required
   	// +kubebuilder:validation:XValidation:rule="self == oldSelf",message="Value is immutable"
	Body string `json:"body,omitempty"`
}

// EmailStatus defines the observed state of Email
type EmailStatus struct {
	// INSERT ADDITIONAL STATUS FIELD - define observed state of cluster
	// Important: Run "make" to regenerate code after modifying this file
	DeliveryStatus string `json:"deliveryStatus,omitempty"`
	MessageId string `json:"messageId,omitempty"`
	Error string `json:"error,omitempty"`
}

//+kubebuilder:object:root=true
//+kubebuilder:subresource:status
type Email struct {
	metav1.TypeMeta   `json:",inline"`
	metav1.ObjectMeta `json:"metadata,omitempty"`

	Spec   EmailSpec   `json:"spec,omitempty"`
	Status EmailStatus `json:"status,omitempty"`
}

//+kubebuilder:object:root=true

// EmailList contains a list of Email
type EmailList struct {
	metav1.TypeMeta `json:",inline"`
	metav1.ListMeta `json:"metadata,omitempty"`
	Items           []Email `json:"items"`
}

func init() {
	SchemeBuilder.Register(&Email{}, &EmailList{})
}
