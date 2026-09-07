namespace DentalSurgery.Domain.Enums;

public enum Gender { Unknown = 0, Male = 1, Female = 2, Other = 3, PreferNotToSay = 4 }

public enum MaritalStatus { Unknown = 0, Single = 1, Married = 2, CivilPartnership = 3, Divorced = 4, Widowed = 5, Separated = 6 }

public enum PatientStatus { Prospective = 0, Active = 1, Inactive = 2, Archived = 3, Transferred = 4, Deceased = 5 }

public enum ContactRelationship
{
    Unknown = 0, Spouse = 1, Partner = 2, Parent = 3, Child = 4, Sibling = 5,
    Guardian = 6, Grandparent = 7, Friend = 8, Carer = 9, SocialWorker = 10, Other = 99
}

public enum ContactRole { Emergency = 0, NextOfKin = 1, Guarantor = 2, Guardian = 3, AuthorisedRepresentative = 4 }

public enum StaffRole
{
    Dentist = 0, DentalHygienist = 1, DentalTherapist = 2, OralSurgeon = 3, Orthodontist = 4,
    Endodontist = 5, Periodontist = 6, Prosthodontist = 7, PaediatricDentist = 8,
    DentalNurse = 9, DentalAssistant = 10, Receptionist = 11, PracticeManager = 12,
    TreatmentCoordinator = 13, Radiographer = 14, Anaesthetist = 15, LabTechnician = 16,
    Administrator = 17, Other = 99
}

public enum EmploymentType { FullTime = 0, PartTime = 1, Associate = 2, Locum = 3, Contractor = 4, Student = 5, Volunteer = 6 }

public enum AlertSeverity { Info = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }

public enum AlertCategory { Medical = 0, Allergy = 1, Financial = 2, Behavioural = 3, Safeguarding = 4, Administrative = 5, Infection = 6 }

public enum SmokingStatus { Never = 0, Former = 1, Occasional = 2, Light = 3, Moderate = 4, Heavy = 5, Vaping = 6, Unknown = 99 }

public enum AlcoholConsumption { None = 0, Occasional = 1, Moderate = 2, Heavy = 3, Unknown = 99 }

public enum OralHygieneRating { Poor = 0, Fair = 1, Good = 2, Excellent = 3 }

public enum ReferralDirection { Inbound = 0, Outbound = 1 }

public enum ReferralStatus { Draft = 0, Sent = 1, Acknowledged = 2, Accepted = 3, Declined = 4, Completed = 5, Cancelled = 6 }

public enum CommunicationChannel { Email = 0, Sms = 1, Phone = 2, Letter = 3, InPerson = 4, PatientPortal = 5, WhatsApp = 6 }

public enum CommunicationDirection { Outbound = 0, Inbound = 1 }

public enum CommunicationStatus { Queued = 0, Sent = 1, Delivered = 2, Failed = 3, Bounced = 4, Read = 5, Responded = 6 }
