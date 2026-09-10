"""
Complete, accurate class diagram of the IRAS domain model — every entity in
IRAS.Domain/Entities (36 classes), read directly from source, laid out in
8 module swimlanes with real relationship connectors.
"""
import os
from PIL import Image, ImageDraw
from diagram_helpers import load_font, wrap_text, save, INK, WHITE

OUT = os.path.join(os.path.dirname(__file__), "figures")

# ---------------------------------------------------------------- data -----
# (class_name, pk_line(s), attr_lines)  -- FK fields annotated inline as (FK->X)
COLUMNS = [
    ("IDENTITY", (238, 238, 238), [
        ("User", ["UserId : int"], [
            "Email : string", "PasswordHash : string?", "AuthProvider : string",
            "Role : UserRole", "IsActive : bool", "LastLogin : DateTime?", "CreatedAt : DateTime",
        ]),
    ]),
    ("CANDIDATE PROFILE & CV", (222, 235, 250), [
        ("CandidateProfile", ["CandidateId : int (PK, FK→User)"], [
            "FirstName : string", "LastName : string", "Citizenship : string?", "Phone : string?",
            "Headline : string?", "ProfilePictureUrl : string?", "GithubUrl : string?",
            "LinkedInUrl : string?", "TotalExpYears : decimal", "EducationLevel : enum",
            "OptInMatching : bool", "CreatedAt : DateTime",
        ]),
        ("Education", ["EducationId : int"], [
            "CandidateId : int (FK)", "Degree : string", "Institution : string",
            "FieldOfStudy : string?", "StartYear : int?", "EndYear : int?", "Grade : string?",
        ]),
        ("WorkExperience", ["ExperienceId : int"], [
            "CandidateId : int (FK)", "CompanyName : string", "JobTitle : string",
            "StartDate : DateTime", "EndDate : DateTime?", "IsCurrent : bool", "Description : string?",
        ]),
        ("Certification", ["CertificationId : int"], [
            "CandidateId : int (FK)", "Name : string", "IssuingOrg : string?", "IssueDate : DateTime?",
            "ExpiryDate : DateTime?", "CertificateFileUrl : string?", "CertificateFileName : string?",
            "CertificateContentType : string?",
        ]),
        ("CandidateLanguage", ["LanguageId : int"], [
            "CandidateId : int (FK)", "LanguageName : string", "Proficiency : string",
        ]),
        ("CandidateProject", ["ProjectId : int"], [
            "CandidateId : int (FK)", "Title : string", "Description : string?", "ProjectUrl : string?",
            "StartDate : DateTime?", "EndDate : DateTime?",
        ]),
        ("Resume", ["ResumeId : int"], [
            "CandidateId : int (FK)", "FileUrl : string", "FileFormat : enum", "FileName : string?",
            "IsPrimary : bool", "ParsedText : string?", "ParseStatus : enum", "ParseError : string?",
            "UploadedAt : DateTime", "SourceCvId : int? (FK→CvDocument)",
        ]),
        ("CvDocument", ["CvId : int"], [
            "CandidateId : int (FK)", "Title : string", "TemplateName : string", "Summary : string?",
            "PhotoUrl : string?", "SectionOrder : string", "CustomizedReferenceTypes : string",
            "CreatedAt : DateTime", "UpdatedAt : DateTime",
        ]),
        ("CvSectionItem", ["CvSectionItemId : int"], [
            "CvId : int (FK)", "ReferenceType : enum", "ReferenceId : int (polymorphic)", "OrderIndex : int",
        ]),
    ]),
    ("SKILLS & DEVELOPMENT", (222, 245, 226), [
        ("Skill", ["SkillId : int"], [
            "SkillName : string", "Category : enum", "Description : string?",
        ]),
        ("SkillAlias", ["AliasId : int"], [
            "SkillId : int (FK)", "AliasText : string", "Source : enum",
        ]),
        ("CandidateSkill", ["CandidateId : int (PK, FK)", "SkillId : int (PK, FK)"], [
            "Proficiency : enum", "YearsExp : decimal", "Source : enum", "IsVerified : bool",
        ]),
        ("SkillResource", ["ResourceId : int"], [
            "SkillId : int (FK)", "Title : string", "Url : string", "ResourceType : enum",
            "Provider : string?", "IsActive : bool", "CreatedBy : int (FK→User)", "CreatedAt : DateTime",
        ]),
        ("CandidateTargetSkill", ["CandidateId : int (PK, FK)", "SkillId : int (PK, FK)"], [
            "Status : enum", "AddedAt : DateTime", "CompletedAt : DateTime?",
        ]),
        ("SkillImprovementPlan", ["PlanId : int"], [
            "CandidateId : int (FK)", "SkillId : int (FK)", "JobId : int? (FK)", "Priority : enum",
            "TargetLevel : enum", "EstimatedDays : int", "Overview : string", "GapReason : string",
            "ProjectTitle : string", "ProjectTask : string", "ProjectExpectedOutput : string",
            "Status : enum", "GeneratedBy : string", "CreatedAt : DateTime",
        ]),
        ("SkillPlanStep", ["StepId : int"], [
            "PlanId : int (FK)", "StepOrder : int", "Title : string", "Description : string",
            "Activity : string", "Output : string", "IsCompleted : bool", "CompletedAt : DateTime?",
        ]),
        ("SkillPlanEvidence", ["EvidenceId : int"], [
            "PlanId : int (FK)", "EvidenceType : enum", "EvidenceUrl : string", "Notes : string?",
            "UploadedAt : DateTime", "VerificationStatus : enum", "VerifiedBy : int? (FK→User)",
            "VerifiedAt : DateTime?", "VerifierNotes : string?", "AiConfidenceScore : int?",
            "AiRationale : string?", "AutoReviewed : bool",
        ]),
    ]),
    ("EMPLOYER & JOBS", (252, 232, 210), [
        ("EmployerProfile", ["EmployerId : int (PK, FK→User)"], [
            "CompanyName : string", "Industry : string?", "CompanySize : enum", "Website : string?",
            "Location : string?", "Description : string?", "LogoUrl : string?", "CreatedAt : DateTime",
        ]),
        ("Job", ["JobId : int"], [
            "EmployerId : int (FK)", "Title : string", "SeniorityLevel : string",
            "RequirementInput : string?", "GeneratedJd : string?", "IsAiGenerated : bool",
            "MinExpYears : int", "EducationReq : enum", "EmploymentType : enum",
            "WorkArrangement : enum", "Location : string?", "Status : enum", "PostedAt : DateTime?",
            "ClosingDate : DateTime?", "TemplateKey : string?", "RequireAssessment : bool",
        ]),
        ("JobRequiredSkill", ["JobId : int (PK, FK)", "SkillId : int (PK, FK)"], [
            "Importance : enum", "Weight : decimal", "MinYears : int",
        ]),
        ("JobMatch", ["MatchId : int"], [
            "JobId : int (FK)", "CandidateId : int (FK)", "MatchScore : decimal",
            "ThresholdPassed : bool", "IsNotified : bool", "MatchedAt : DateTime",
        ]),
    ]),
    ("APPLICATIONS & INTERVIEWS", (232, 222, 248), [
        ("Application", ["ApplicationId : int"], [
            "CandidateId : int (FK)", "JobId : int (FK)", "ResumeId : int (FK)", "Status : enum",
            "TotalScore : decimal", "SkillMatch : decimal", "ExperienceMatch : decimal",
            "EducationMatch : decimal", "SemanticSimilarity : decimal", "AssessmentScore : decimal?",
            "AppliedAt : DateTime", "UpdatedAt : DateTime?",
        ]),
        ("ApplicationStatusHistory", ["HistoryId : int"], [
            "ApplicationId : int (FK)", "OldStatus : enum", "NewStatus : enum",
            "ChangedBy : int (FK→User)", "ChangedAt : DateTime",
        ]),
        ("SkillGap", ["GapId : int"], [
            "ApplicationId : int (FK)", "SkillId : int (FK)", "Importance : enum",
            "Suggestion : string?", "DetectedAt : DateTime",
        ]),
        ("Interview", ["InterviewId : int"], [
            "ApplicationId : int (FK)", "ScheduledBy : int (FK→User)", "ScheduledAt : DateTime",
            "DurationMinutes : int", "Mode : enum", "Location : string?", "MeetingLink : string?",
            "InterviewerNames : string?", "Notes : string?", "Status : enum",
            "CancellationReason : string?", "CreatedAt : DateTime", "UpdatedAt : DateTime?",
        ]),
        ("Feedback", ["FeedbackId : int"], [
            "ApplicationId : int (FK, 1:1)", "MessageText : string", "ApprovedBy : int? (FK→User)",
            "ApprovalStatus : enum", "DeliveryStatus : enum", "Channel : enum",
            "GeneratedAt : DateTime", "SentAt : DateTime?",
        ]),
    ]),
    ("ASSESSMENTS", (222, 244, 244), [
        ("JobAssessment", ["JobAssessmentId : int"], [
            "JobId : int (FK, 1:1)", "GeneratedAt : DateTime", "GeneratedBy : string",
        ]),
        ("AssessmentQuestion", ["AssessmentQuestionId : int"], [
            "JobAssessmentId : int (FK)", "SkillId : int? (FK)", "QuestionType : enum",
            "QuestionText : string", "Options : List<string> (JSON)", "CorrectOptionIndex : int",
            "ModelAnswer : string?", "QuestionOrder : int",
        ]),
        ("CandidateAssessmentAttempt", ["AttemptId : int"], [
            "CandidateId : int (FK)", "JobId : int (FK)", "JobAssessmentId : int (FK)",
            "Status : enum", "Score : decimal?", "StartedAt : DateTime", "CompletedAt : DateTime?",
        ]),
        ("CandidateAssessmentAnswer", ["AnswerId : int"], [
            "AttemptId : int (FK)", "AssessmentQuestionId : int (FK)", "SelectedOptionIndex : int?",
            "FreeTextAnswer : string?", "ScoreFraction : decimal",
        ]),
    ]),
    ("ENGAGEMENT", (250, 224, 236), [
        ("Notification", ["NotificationId : int"], [
            "UserId : int (FK)", "Type : enum", "Title : string", "Message : string",
            "Channel : enum", "RelatedEntityType : enum?", "RelatedEntityId : int?",
            "IsRead : bool", "CreatedAt : DateTime",
        ]),
        ("ChatConversation", ["ConversationId : int"], [
            "UserId : int (FK)", "Topic : string?", "StartedAt : DateTime", "LastActivity : DateTime",
        ]),
        ("ChatMessage", ["MessageId : int"], [
            "ConversationId : int (FK)", "Sender : enum", "Content : string", "Intent : string?",
            "CreatedAt : DateTime",
        ]),
    ]),
    ("ADMIN & GOVERNANCE", (250, 240, 210), [
        ("KnowledgeBase", ["KbId : int"], [
            "Title : string", "Content : string", "Category : enum", "IsActive : bool",
            "UpdatedBy : int (FK→User)", "UpdatedAt : DateTime",
        ]),
        ("AuditLog", ["LogId : int"], [
            "UserId : int (FK)", "Action : string", "EntityType : string", "EntityId : int",
            "IpAddress : string?", "Details : string?", "CreatedAt : DateTime",
        ]),
    ]),
]

# Backbone structural relationships drawn as connectors: (from, to, card_from, card_to)
EDGES = [
    ("User", "CandidateProfile", "1", "0..1"),
    ("User", "EmployerProfile", "1", "0..1"),
    ("CandidateProfile", "Resume", "1", "*"),
    ("CandidateProfile", "Education", "1", "*"),
    ("CandidateProfile", "WorkExperience", "1", "*"),
    ("CandidateProfile", "Certification", "1", "*"),
    ("CandidateProfile", "CandidateLanguage", "1", "*"),
    ("CandidateProfile", "CandidateProject", "1", "*"),
    ("CandidateProfile", "CvDocument", "1", "*"),
    ("CvDocument", "CvSectionItem", "1", "*"),
    ("CvDocument", "Resume", "1", "0..*"),
    ("CandidateProfile", "CandidateSkill", "1", "*"),
    ("Skill", "CandidateSkill", "1", "*"),
    ("Skill", "SkillAlias", "1", "*"),
    ("Skill", "SkillResource", "1", "*"),
    ("CandidateProfile", "CandidateTargetSkill", "1", "*"),
    ("Skill", "CandidateTargetSkill", "1", "*"),
    ("CandidateProfile", "SkillImprovementPlan", "1", "*"),
    ("Skill", "SkillImprovementPlan", "1", "*"),
    ("Job", "SkillImprovementPlan", "0..1", "*"),
    ("SkillImprovementPlan", "SkillPlanStep", "1", "*"),
    ("SkillImprovementPlan", "SkillPlanEvidence", "1", "*"),
    ("EmployerProfile", "Job", "1", "*"),
    ("Job", "JobRequiredSkill", "1", "*"),
    ("Skill", "JobRequiredSkill", "1", "*"),
    ("Job", "JobMatch", "1", "*"),
    ("CandidateProfile", "JobMatch", "1", "*"),
    ("CandidateProfile", "Application", "1", "*"),
    ("Job", "Application", "1", "*"),
    ("Resume", "Application", "1", "*"),
    ("Application", "SkillGap", "1", "*"),
    ("Skill", "SkillGap", "1", "*"),
    ("Application", "ApplicationStatusHistory", "1", "*"),
    ("Application", "Interview", "1", "*"),
    ("Application", "Feedback", "1", "0..1"),
    ("Job", "JobAssessment", "1", "0..1"),
    ("JobAssessment", "AssessmentQuestion", "1", "*"),
    ("Skill", "AssessmentQuestion", "0..1", "*"),
    ("CandidateProfile", "CandidateAssessmentAttempt", "1", "*"),
    ("Job", "CandidateAssessmentAttempt", "1", "*"),
    ("JobAssessment", "CandidateAssessmentAttempt", "1", "*"),
    ("CandidateAssessmentAttempt", "CandidateAssessmentAnswer", "1", "*"),
    ("AssessmentQuestion", "CandidateAssessmentAnswer", "1", "*"),
    ("User", "Notification", "1", "*"),
    ("User", "ChatConversation", "1", "*"),
    ("ChatConversation", "ChatMessage", "1", "*"),
]

FONT_TITLE = load_font(13, bold=True)
FONT_ATTR = load_font(9.5)
COL_W = 470
COL_GAP = 74
BOX_GAP = 22
PAD = 10
LEFT_MARGIN = 40
TOP_MARGIN = 190


def compute_box_height(pk_lines, attr_lines):
    return PAD * 2 + 20 + 4 + len(pk_lines) * 14 + 3 + len(attr_lines) * 13.5 + 6


def main():
    # Pass 1: compute column content heights and box registry (positions)
    registry = {}  # class name -> (x, y, w, h, column_index)
    col_x = LEFT_MARGIN
    col_heights = []
    layout = []  # (col_index, col_x, header_text, header_color, boxes[(name, pk, attrs, y, h)])
    for ci, (header, color, classes) in enumerate(COLUMNS):
        y = TOP_MARGIN
        boxes = []
        for name, pk_lines, attr_lines in classes:
            h = compute_box_height(pk_lines, attr_lines)
            boxes.append((name, pk_lines, attr_lines, y, h))
            registry[name] = (col_x, y, COL_W, h, ci)
            y += h + BOX_GAP
        col_heights.append(y)
        layout.append((ci, col_x, header, color, boxes))
        col_x += COL_W + COL_GAP

    canvas_w = col_x - COL_GAP + LEFT_MARGIN
    canvas_h = max(col_heights) + 60
    img = Image.new("RGB", (int(canvas_w), int(canvas_h)), WHITE)
    dr = ImageDraw.Draw(img)

    # Title
    f_title = load_font(26, bold=True)
    title = "IRAS — Complete Domain Model Class Diagram (36 Entities)"
    dr.text((canvas_w/2 - dr.textlength(title, font=f_title)/2, 18), title, font=f_title, fill=INK)
    f_sub = load_font(13)
    sub = "Every persistent entity in IRAS.Domain/Entities, grouped by module; connectors show real foreign-key relationships (1 / 0..1 / *)"
    dr.text((canvas_w/2 - dr.textlength(sub, font=f_sub)/2, 52), sub, font=f_sub, fill=(90, 90, 90))
    note = "Single-field \"actor\" references (CreatedBy, VerifiedBy, ChangedBy, ScheduledBy, ApprovedBy, UpdatedBy → User) are shown as (FK→User) inline on the attribute rather than as a separate connector, to keep the diagram legible."
    f_note = load_font(11)
    dr.text((canvas_w/2 - dr.textlength(note, font=f_note)/2, 74), note, font=f_note, fill=(120, 120, 120))

    # Draw column headers
    for ci, col_x, header, color, boxes in layout:
        dr.rectangle([col_x, 118, col_x + COL_W, 168], fill=color, outline=INK, width=2)
        f = load_font(14, bold=True)
        for j, line in enumerate(wrap_text(dr, header, f, COL_W - 16)):
            tw = dr.textlength(line, font=f)
            dr.text((col_x + COL_W/2 - tw/2, 126 + j*18), line, font=f, fill=INK)

    # Draw relationship connectors first (under the boxes)
    for a, b, ca, cb in EDGES:
        if a not in registry or b not in registry:
            continue
        ax, ay, aw, ah, aci = registry[a]
        bx, by, bw, bh, bci = registry[b]
        if aci == bci:
            # same column: short curve on the right side
            y1 = ay + ah * 0.5
            y2 = by + bh * 0.5
            x = ax + aw
            dr.line([(x, y1), (x + 18, y1), (x + 18, y2), (x, y2)], fill=(140, 140, 150), width=1)
            dr.text((x + 4, min(y1, y2) + abs(y2 - y1) / 2 - 10), ca, font=FONT_ATTR, fill=(90, 90, 90))
        else:
            y1 = ay + ah * 0.5
            y2 = by + bh * 0.5
            if bci > aci:
                p1 = (ax + aw, y1)
                p2 = (bx, y2)
            else:
                p1 = (ax, y1)
                p2 = (bx + bw, y2)
            dr.line([p1, p2], fill=(150, 150, 165), width=1)
            dr.text((p1[0] + (6 if p2[0] > p1[0] else -18), p1[1] - 12), ca, font=FONT_ATTR, fill=(100, 100, 110))
            dr.text((p2[0] + (-24 if p2[0] > p1[0] else 6), p2[1] - 12), cb, font=FONT_ATTR, fill=(100, 100, 110))

    # Draw class boxes on top
    for ci, col_x, header, color, boxes in layout:
        for name, pk_lines, attr_lines, y, h in boxes:
            x = col_x
            dr.rectangle([x, y, x + COL_W, y + h], fill=WHITE, outline=INK, width=2)
            ty = y + PAD
            tw = dr.textlength(name, font=FONT_TITLE)
            dr.text((x + COL_W/2 - tw/2, ty), name, font=FONT_TITLE, fill=INK)
            ty += 20
            dr.line([(x, y + ty - y - 2), (x + COL_W, y + ty - y - 2)], fill=INK, width=1)
            ty += 4
            for line in pk_lines:
                dr.text((x + PAD, ty), line, font=FONT_ATTR, fill=(150, 40, 40))
                ty += 14
            ty += 3
            dr.line([(x + 4, ty - 2), (x + COL_W - 4, ty - 2)], fill=(210, 210, 210), width=1)
            for line in attr_lines:
                dr.text((x + PAD, ty), line, font=FONT_ATTR, fill=INK)
                ty += 13.5

    save(img, os.path.join(OUT, "class_diagram_full.png"))
    print("Canvas size:", canvas_w, "x", canvas_h)
    print("Classes drawn:", sum(len(c) for _, _, c in COLUMNS))
    print("Edges drawn:", len(EDGES))


if __name__ == "__main__":
    main()
