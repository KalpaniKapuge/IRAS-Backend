"""Generates hand-authored box/arrow diagrams (PIL) for the IRAS thesis."""
import os
from diagram_helpers import (new_canvas, box, arrow, label, actor, ellipse_usecase,
                              load_font, save, INK, ACCENT, ACCENT_FILL, GREY_FILL, WHITE)

OUT = os.path.join(os.path.dirname(__file__), "figures")
os.makedirs(OUT, exist_ok=True)


def d_architecture():
    W, H = 1300, 950
    img, dr = new_canvas(W, H)
    f_title = load_font(20, bold=True)
    dr.text((W/2 - dr.textlength("IRAS Layered System Architecture", font=f_title)/2, 20),
            "IRAS Layered System Architecture", font=f_title, fill=INK)

    layers = [
        ("PRESENTATION LAYER", "Candidate Portal  |  Employer Portal  |  Admin Portal\n(React + TypeScript, Vite SPA)", ACCENT_FILL),
        ("API LAYER (IRAS.API)", "Controllers (Jobs, Applications, CandidateProfile, EmployerJobs, Assessments, Chat, Admin*) | JWT Bearer Auth | Swagger/OpenAPI | Exception Filter", WHITE),
        ("APPLICATION LAYER (IRAS.Application)", "Business services: JobMatchingService, ScoringService, SkillGapService, SkillImprovementPlanService,\nCvService, ChatService, AssessmentService, ReportingService, AuditLogService", WHITE),
        ("DOMAIN LAYER (IRAS.Domain)", "Entities: User, CandidateProfile, EmployerProfile, Resume, Skill, Job, Application, JobMatch,\nSkillGap, SkillImprovementPlan, JobAssessment, Interview, Notification", ACCENT_FILL),
        ("INFRASTRUCTURE LAYER (IRAS.Infrastructure)", "EF Core + SQL Server | Supabase / Local File Storage | AiServiceClient (HTTP) | Email Sender", WHITE),
    ]
    x, w = 80, W - 160
    y = 70
    lh = 130
    for i, (title, sub, fill) in enumerate(layers):
        box(dr, x, y, w, lh - 15, title=title, subtitle=sub, fill=fill,
            title_font=load_font(16, bold=True), sub_font=load_font(12))
        if i < len(layers) - 1:
            arrow(dr, (x + w/2, y + lh - 15), (x + w/2, y + lh), color=INK, width=2)
        y += lh

    # external AI service box, connected from Application layer
    ai_x, ai_y, ai_w, ai_h = x + w + 20 - 380, y + 40, 340, 110
    box(dr, W - 380, 260, 300, 130, title="EXTERNAL AI SERVICE",
        subtitle="Python FastAPI (Uvicorn)\nResume parsing | Semantic ranking\nTrained fit-classifier | /health",
        fill=GREY_FILL, title_font=load_font(14, bold=True), sub_font=load_font(11))
    arrow(dr, (x + w - 5, 260 + 65), (W - 380, 260 + 65), color=ACCENT, width=2)
    label(dr, (x + w - 5 + (W - 380 - (x + w - 5))/2), 250, "HTTP", font=load_font(10, bold=True))

    save(img, os.path.join(OUT, "architecture_diagram.png"))


def d_use_case():
    W, H = 1400, 1000
    img, dr = new_canvas(W, H)
    f_title = load_font(20, bold=True)
    dr.text((W/2 - dr.textlength("Use Case Diagram — IRAS", font=f_title)/2, 15),
            "Use Case Diagram — IRAS", font=f_title, fill=INK)

    dr.rounded_rectangle([260, 60, 1140, 940], radius=14, outline=INK, width=2)
    f_sys = load_font(14, bold=True)
    dr.text((700 - dr.textlength("Intelligent Recruitment Automation System", font=f_sys)/2, 68),
            "Intelligent Recruitment Automation System", font=f_sys, fill=INK)

    actor(dr, 90, 380, "Candidate")
    actor(dr, 1300, 260, "Employer")
    actor(dr, 1300, 700, "Administrator")

    candidate_uc = [
        "Register / Login", "Manage Candidate Profile", "Upload & Parse Resume",
        "Build CV (PDF)", "Search & Apply for Jobs", "View Match Score",
        "View Skill Gap & Plan", "Submit Skill Evidence", "Use Recruitment Chatbot",
        "View Notifications",
    ]
    employer_uc = [
        "Manage Company Profile", "Generate Job Description (AI)", "Post / Publish Job",
        "Review & Rank Applicants", "Schedule Interview", "Attach Skill Assessment",
        "Give Candidate Feedback",
    ]
    admin_uc = [
        "Manage Users", "Moderate Job Posts", "Manage Skill Taxonomy",
        "Manage Knowledge Base", "Review Audit Log", "Monitor AI Service Health",
        "Generate Reports",
    ]

    def place(items, x0, y0, dy):
        y = y0
        for t in items:
            ellipse_usecase(dr, x0, y, 300, 46, t)
            y += dy
        return y

    ys_c = [110 + i * 82 for i in range(len(candidate_uc))]
    for t, yy in zip(candidate_uc, ys_c):
        ellipse_usecase(dr, 480, yy, 260, 46, t)
        arrow(dr, (120, 380), (350, yy), color=INK, width=1)

    ys_e = [110 + i * 100 for i in range(len(employer_uc))]
    for t, yy in zip(employer_uc, ys_e):
        ellipse_usecase(dr, 900, yy, 260, 46, t)
        arrow(dr, (1270, 260), (1030, yy), color=INK, width=1)

    ys_a = [110 + i * 82 for i in range(len(admin_uc))]
    for i, (t, yy) in enumerate(zip(admin_uc, ys_a)):
        yy2 = 500 + i * 62
        ellipse_usecase(dr, 900, yy2, 260, 44, t, font=load_font(10))
        arrow(dr, (1270, 700), (1030, yy2), color=INK, width=1)

    save(img, os.path.join(OUT, "use_case_diagram.png"))


def d_class_diagram():
    W, H = 1500, 1050
    img, dr = new_canvas(W, H)
    f_title = load_font(20, bold=True)
    dr.text((W/2 - dr.textlength("Class Diagram — Core Domain Entities", font=f_title)/2, 12),
            "Class Diagram — Core Domain Entities", font=f_title, fill=INK)

    def cls(x, y, w, h, name, attrs):
        dr.rectangle([x, y, x + w, y + h], fill=WHITE, outline=INK, width=2)
        f_n = load_font(13, bold=True)
        dr.text((x + w/2 - dr.textlength(name, font=f_n)/2, y + 6), name, font=f_n, fill=INK)
        dr.line([(x, y + 28), (x + w, y + 28)], fill=INK, width=2)
        f_a = load_font(11)
        yy = y + 34
        for a in attrs:
            dr.text((x + 8, yy), a, font=f_a, fill=INK)
            yy += 15
        return (x, y, w, h)

    User = cls(40, 60, 220, 110, "User", ["userId : int", "email : string", "role : UserRole", "authProvider : string"])
    Cand = cls(40, 220, 240, 140, "CandidateProfile", ["candidateId : int", "githubUrl : string", "linkedInUrl : string", "optInMatching : bool"])
    Emp = cls(40, 420, 240, 110, "EmployerProfile", ["employerId : int", "companyName : string", "companySize : enum"])
    Resume = cls(340, 220, 220, 110, "Resume", ["resumeId : int", "isPrimary : bool", "parsedText : string"])
    Skill = cls(640, 60, 220, 110, "Skill", ["skillId : int", "name : string", "category : enum"])
    CandSkill = cls(340, 60, 220, 110, "CandidateSkill", ["candidateId : FK", "skillId : FK", "proficiency : enum"])
    Job = cls(960, 60, 240, 130, "Job", ["jobId : int", "title : string", "status : JobStatus", "workArrangement : enum"])
    JobReqSkill = cls(940, 240, 260, 100, "JobRequiredSkill", ["jobId : FK", "skillId : FK", "importance : enum"])
    App = cls(340, 420, 240, 130, "Application", ["applicationId : int", "status : ApplicationStatus", "totalMarks : decimal"])
    JobMatch = cls(640, 420, 240, 130, "JobMatch", ["matchId : int", "matchScore : decimal", "thresholdPassed : bool"])
    Gap = cls(940, 420, 260, 110, "SkillGap", ["applicationId : FK", "skillId : FK", "gapLevel : enum"])
    Plan = cls(340, 620, 260, 130, "SkillImprovementPlan", ["planId : int", "targetLevel : enum", "status : SkillPlanStatus"])
    Evid = cls(640, 620, 260, 130, "SkillPlanEvidence", ["evidenceId : int", "evidenceType : enum", "verificationStatus : enum"])
    Assess = cls(940, 620, 260, 130, "JobAssessment", ["assessmentId : int", "jobId : FK", "passMarkPercent : int"])
    Interview = cls(940, 800, 260, 110, "Interview", ["interviewId : int", "mode : enum", "status : InterviewStatus"])

    rels = [
        (User, Cand, "1..1"), (User, Emp, "1..1"), (Cand, Resume, "1..*"),
        (Cand, CandSkill, "1..*"), (Skill, CandSkill, "1..*"), (Job, JobReqSkill, "1..*"),
        (Skill, JobReqSkill, "1..*"), (Cand, App, "1..*"), (Job, App, "1..*"),
        (Cand, JobMatch, "1..*"), (Job, JobMatch, "1..*"), (App, Gap, "1..*"),
        (Cand, Plan, "1..*"), (Plan, Evid, "1..*"), (Job, Assess, "0..1"),
        (App, Interview, "0..1"),
    ]

    def edge(a, b, txt):
        ax, ay, aw, ah = a
        bx, by, bw, bh = b
        p1 = (ax + aw/2, ay + ah)
        p2 = (bx + bw/2, by)
        if abs(p1[0]-p2[0]) < 5 and abs(ay - by) < 5:
            p1 = (ax + aw, ay + ah/2); p2 = (bx, by + bh/2)
        dr.line([p1, p2], fill=(120, 120, 120), width=1)
        mx, my = (p1[0]+p2[0])/2, (p1[1]+p2[1])/2
        label(dr, mx, my, txt, font=load_font(9), bg=WHITE)

    for a, b, t in rels:
        edge(a, b, t)

    save(img, os.path.join(OUT, "class_diagram.png"))


def d_activity_diagram():
    W, H = 1000, 1500
    img, dr = new_canvas(W, H)
    f_title = load_font(20, bold=True)
    title = "Activity Diagram — Application Shortlisting & Candidate Ranking"
    for i, line in enumerate([title[:38], title[38:]]):
        dr.text((W/2 - dr.textlength(line, font=f_title)/2, 12 + i*26), line, font=f_title, fill=INK)

    def oval(y, text, w=380, h=54, fill=ACCENT_FILL):
        x = W/2 - w/2
        dr.ellipse([x, y, x+w, y+h], fill=fill, outline=INK, width=2)
        f = load_font(13)
        tw = dr.textlength(text, font=f)
        dr.text((W/2 - tw/2, y + h/2 - 8), text, font=f, fill=INK)
        return (W/2, y+h)

    def rect(y, text, w=460, h=54, fill=WHITE):
        x = W/2 - w/2
        dr.rectangle([x, y, x+w, y+h], fill=fill, outline=INK, width=2)
        f = load_font(12)
        tw = dr.textlength(text, font=f)
        dr.text((W/2 - tw/2, y + h/2 - 8), text, font=f, fill=INK)
        return (W/2, y+h)

    def diamond(y, text, w=340, h=80):
        x = W/2 - w/2
        dr.polygon([(W/2, y), (x+w, y+h/2), (W/2, y+h), (x, y+h/2)], fill=(255, 250, 230), outline=INK, width=2)
        f = load_font(11)
        for j, line in enumerate(text.split("\n")):
            tw = dr.textlength(line, font=f)
            dr.text((W/2 - tw/2, y + h/2 - 16 + j*14), line, font=f, fill=INK)
        return (W/2, y+h)

    y = 80
    p = oval(y, "Start: Candidate submits application"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Extract skills from parsed resume text"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Compute Skill Match (taxonomy comparison)"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Call AI service: semantic similarity + ML fit score"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = diamond(y, "Does the job\nrequire an assessment?"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    label(dr, W/2 + 130, y - 65, "yes", font=load_font(11, bold=True))
    p = rect(y, "Candidate completes assessment → AI grades answers", fill=ACCENT_FILL); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Compute weighted Total Marks (skill, assessment, experience, education, semantic)"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Persist Application score + SkillGap records"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = rect(y, "Employer views ranked, explainable applicant list"); y = p[1] + 25
    arrow(dr, p, (p[0], y), color=INK)
    p = oval(y, "End", w=200, fill=GREY_FILL)

    save(img, os.path.join(OUT, "activity_diagram.png"))


def _seq_base(W, H, title, lifelines):
    img, dr = new_canvas(W, H)
    f_title = load_font(19, bold=True)
    dr.text((W/2 - dr.textlength(title, font=f_title)/2, 10), title, font=f_title, fill=INK)
    n = len(lifelines)
    margin = 90
    usable = W - 2 * margin
    xs = [margin + usable * i / (n - 1) for i in range(n)]
    top = 60
    for x, name in zip(xs, lifelines):
        box(dr, x - 85, top, 170, 40, title=name, title_font=load_font(12, bold=True))
        dr.line([(x, top + 40), (x, H - 30)], fill=(140, 140, 140), width=1)
    return img, dr, xs, top + 40


def d_sequence_matching():
    life = ["Candidate", "ApplicationsController", "ApplicationService", "ScoringService", "AI Service (FastAPI)", "Database"]
    img, dr, xs, y0 = _seq_base(1500, 620, "Sequence Diagram — Job Application & Scoring", life)
    steps = [
        (0, 1, "POST /applications", False),
        (1, 2, "CreateApplicationAsync()", False),
        (2, 3, "ComputeMatchSignalsAsync()", False),
        (3, 4, "POST /rank (resume, job text, taxonomy)", False),
        (4, 3, "semanticSimilarity, fitScore", True),
        (3, 3, "ComputeSkillMatch() [local taxonomy]", False),
        (3, 2, "MatchSignals", True),
        (2, 5, "INSERT Application (scores, SkillGap rows)", False),
        (5, 2, "OK", True),
        (2, 1, "ApplicationDto", True),
        (1, 0, "201 Created", True),
    ]
    y = y0 + 30
    for a, b, text, ret in steps:
        arrow(dr, (xs[a], y), (xs[b], y), color=ACCENT if not ret else (120, 120, 120), width=2, dashed=ret)
        mx = (xs[a] + xs[b]) / 2
        label(dr, mx, y - 12, text, font=load_font(10))
        y += 44
    save(img, os.path.join(OUT, "sequence_matching.png"))


def d_sequence_resume():
    life = ["Candidate", "CvController", "CvService", "AI Service (FastAPI)", "File Storage", "Database"]
    img, dr, xs, y0 = _seq_base(1500, 600, "Sequence Diagram — Resume Upload & Parsing", life)
    steps = [
        (0, 1, "POST /resumes (file)", False),
        (1, 2, "UploadResumeAsync(file)", False),
        (2, 4, "Save(fileStream)", False),
        (4, 2, "storagePath", True),
        (2, 3, "POST /parse-resume (file, taxonomy)", False),
        (3, 2, "parsedText, detectedSkills[]", True),
        (2, 5, "INSERT Resume, CandidateSkill rows", False),
        (5, 2, "OK", True),
        (2, 1, "ParseResumeResult", True),
        (1, 0, "200 OK (preview for review)", True),
    ]
    y = y0 + 30
    for a, b, text, ret in steps:
        arrow(dr, (xs[a], y), (xs[b], y), color=ACCENT if not ret else (120, 120, 120), width=2, dashed=ret)
        mx = (xs[a] + xs[b]) / 2
        label(dr, mx, y - 12, text, font=load_font(10))
        y += 46
    save(img, os.path.join(OUT, "sequence_resume.png"))


def d_sequence_evidence():
    life = ["Candidate", "SkillImprovementPlansController", "SkillPlanEvidenceService", "AI Evidence Reviewer", "Administrator", "Database"]
    img, dr, xs, y0 = _seq_base(1650, 660, "Sequence Diagram — Skill Evidence Submission & Review", life)
    steps = [
        (0, 1, "POST /evidence (file/link)", False),
        (1, 2, "SubmitEvidenceForReviewAsync()", False),
        (2, 5, "status = Pending", False),
        (2, 3, "ReviewEvidenceAsync(evidence, planStep)", False),
        (3, 2, "recommendation, confidence", True),
        (2, 4, "flag for manual decision (low confidence)", False),
        (4, 1, "PUT /evidence/{id}/decision", False),
        (1, 2, "ApplyDecisionAsync(Approved/Rejected)", False),
        (2, 5, "UPDATE EvidenceVerificationStatus", False),
        (2, 0, "notify candidate", True),
    ]
    y = y0 + 30
    for a, b, text, ret in steps:
        arrow(dr, (xs[a], y), (xs[b], y), color=ACCENT if not ret else (120, 120, 120), width=2, dashed=ret)
        mx = (xs[a] + xs[b]) / 2
        label(dr, mx, y - 12, text, font=load_font(10))
        y += 48
    save(img, os.path.join(OUT, "sequence_evidence.png"))


def d_dsrm_workflow():
    W, H = 1500, 320
    img, dr = new_canvas(W, H)
    f_title = load_font(19, bold=True)
    dr.text((30, 15), "Research Methodology Execution Workflow (Design Science Research)", font=f_title, fill=INK)
    stages = [
        ("Problem\nIdentification", "Recruitment pain points"),
        ("Relevance\nJustification", "AI adoption + skill-gap evidence"),
        ("Comparative\nAnalysis", "Literature + system comparison"),
        ("Objectives\nFinalization", "Research-specific objectives"),
        ("Design &\nDevelopment", "Prototype + data handling"),
        ("Evaluation &\nCommunication", "Metrics, user feedback, thesis"),
    ]
    n = len(stages)
    margin = 30
    gap = 20
    w = (W - 2*margin - gap*(n-1)) / n
    x = margin
    y = 90
    for i, (title, sub) in enumerate(stages):
        box(dr, x, y, w, 130, title=title, subtitle=sub, fill=ACCENT_FILL,
            title_font=load_font(13, bold=True), sub_font=load_font(10))
        if i < n - 1:
            arrow(dr, (x + w, y + 65), (x + w + gap, y + 65), color=INK, width=2)
        x += w + gap
    save(img, os.path.join(OUT, "dsrm_workflow.png"))


def d_conceptual_map():
    W, H = 1400, 950
    img, dr = new_canvas(W, H)
    f_title = load_font(19, bold=True)
    dr.text((W/2 - dr.textlength("Conceptual Map of the Literature", font=f_title)/2, 12),
            "Conceptual Map of the Literature", font=f_title, fill=INK)
    cx, cy = W/2, H/2
    box(dr, cx-170, cy-55, 340, 110, title="AI-SUPPORTED INTEGRATED\nRECRUITMENT AUTOMATION SYSTEM",
        fill=ACCENT_FILL, title_font=load_font(13, bold=True))
    cats = [
        ("Resume Screening\n& Parsing", "NLP/ML classification, transformer models (S-BERT), format handling gaps"),
        ("Automatic Job\nDescription Generation", "Capability-aware neural nets; rarely linked to screening/ranking"),
        ("User Profiling &\nCandidate Insight", "Feature fusion for person-job fit; mostly static resume data"),
        ("AI Governance &\nTransparency", "FAIRE bias benchmark; need explainable scoring & audit trails"),
        ("Semantic Job-\nCandidate Matching", "ML-based person-job matching; lacks integrated ranking/shortlisting"),
        ("Recruitment\nChatbot Support", "Candidate Q&A; limited to simple queries, one recruitment phase"),
        ("Skill Gap Analysis\n& Feedback", "AI feedback loops; weak skill-gap visualization, inconsistent feedback"),
        ("Automated Notifications\n& Scheduling", "Person-job fit alerts; mostly reactive, not integrated across roles"),
    ]
    import math
    R = 330
    for i, (title, sub) in enumerate(cats):
        ang = -math.pi/2 + i * (2*math.pi/len(cats))
        bx = cx + R*math.cos(ang) - 150
        by = cy + R*math.sin(ang) - 55
        box(dr, bx, by, 300, 110, title=title, subtitle=sub, fill=WHITE,
            title_font=load_font(12, bold=True), sub_font=load_font(9.5))
        arrow(dr, (cx + 150*math.cos(ang), cy + 45*math.sin(ang)*1.2), (bx+150, by+55), color=(150, 150, 150), width=1)
    save(img, os.path.join(OUT, "conceptual_map.png"))


def d_rich_picture():
    W, H = 1500, 950
    img, dr = new_canvas(W, H)
    f_title = load_font(20, bold=True)
    dr.text((W/2 - dr.textlength("Rich Picture of the Proposed System", font=f_title)/2, 12),
            "Rich Picture of the Proposed System", font=f_title, fill=INK)

    box(dr, 560, 400, 380, 160, title="INTELLIGENT RECRUITMENT\nAUTOMATION SYSTEM",
        subtitle="Candidate Portal | Employer Portal | Admin Portal\nAuthentication + Role-based Access",
        fill=ACCENT_FILL, title_font=load_font(15, bold=True), sub_font=load_font(11))

    cand = box(dr, 50, 90, 340, 190, title="CANDIDATE USERS",
               subtitle="Profile & resume management\nApply to jobs & auto-matching\nSkill gap identification & CV generation\nSkill dev. resources, feedback, chatbot",
               fill=(232, 245, 233), title_font=load_font(13, bold=True), sub_font=load_font(10.5))
    emp = box(dr, 1100, 90, 350, 190, title="EMPLOYER / COMPANY USERS",
              subtitle="Company profile & AI job-description gen.\nPost jobs & review applications\nShortlist, rank, compare candidates\nSchedule interviews, candidate feedback",
              fill=(255, 243, 224), title_font=load_font(13, bold=True), sub_font=load_font(10.5))
    admin = box(dr, 575, 90, 350, 170, title="ADMIN USERS",
                subtitle="User & job post governance\nSkill taxonomy & knowledge base\nAudit log review, report generation\nSystem status / AI health monitoring",
                fill=(255, 235, 238), title_font=load_font(13, bold=True), sub_font=load_font(10.5))

    ai = box(dr, 60, 700, 330, 150, title="AI SERVICE LAYER",
             subtitle="LLM: JD generation, feedback,\nskill-gap explanation, chatbot\nPython service: resume parsing (NER),\nsemantic ranking, fit-classifier",
             fill=GREY_FILL, title_font=load_font(12, bold=True), sub_font=load_font(10))
    db = box(dr, 450, 700, 260, 150, title="DATABASE & FILE STORAGE",
             subtitle="Candidate/Employer/Job records\nApplications, matches, skill gaps\nResumes, CVs, assessments",
             fill=WHITE, title_font=load_font(12, bold=True), sub_font=load_font(10))
    tax = box(dr, 760, 700, 300, 150, title="SKILL TAXONOMY &\nKNOWLEDGE BASE",
              subtitle="Aliases / synonyms mapping\nFAQs, policy guidance, skill advice",
              fill=WHITE, title_font=load_font(12, bold=True), sub_font=load_font(10))
    rank = box(dr, 1110, 700, 340, 150, title="RANKING • MATCHING •\nSKILL GAP • FEEDBACK ENGINE",
               subtitle="Weighted scoring, structured feedback,\ncandidate notifications, comparison",
               fill=WHITE, title_font=load_font(12, bold=True), sub_font=load_font(10))

    arrow(dr, (220, 280), (620, 400), color=INK, width=2)
    label(dr, 400, 330, "uploads resume / applies", font=load_font(10, bold=True))
    arrow(dr, (1270, 280), (940, 400), color=INK, width=2)
    label(dr, 1080, 330, "posts jobs / reviews ranking", font=load_font(10, bold=True))
    arrow(dr, (750, 260), (750, 400), color=INK, width=2)
    label(dr, 850, 330, "governance & monitoring", font=load_font(10, bold=True))

    arrow(dr, (620, 560), (225, 700), color=(150, 150, 150), width=1)
    arrow(dr, (700, 560), (580, 700), color=(150, 150, 150), width=1)
    arrow(dr, (800, 560), (910, 700), color=(150, 150, 150), width=1)
    arrow(dr, (900, 560), (1280, 700), color=(150, 150, 150), width=1)

    save(img, os.path.join(OUT, "rich_picture.png"))


def d_timeline():
    W, H = 1500, 500
    img, dr = new_canvas(W, H)
    f_title = load_font(19, bold=True)
    dr.text((30, 12), "Project Timeline (Gantt Overview)", font=f_title, fill=INK)
    months = ["Nov", "Dec", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep"]
    tasks = [
        ("1. Inception & Planning", 0, 2, (52, 111, 173)),
        ("   Topic Selection & Finalize", 0, 1, (52, 111, 173)),
        ("   Literature Review & Problem Def.", 0, 2, (137, 176, 219)),
        ("   Proposal Writing & Submission", 1, 2, (137, 176, 219)),
        ("2. Data Acquisition & Preparation", 2, 4, (76, 153, 76)),
        ("   Dataset Collection", 2, 3, (76, 153, 76)),
        ("   Data Cleaning & Preprocessing", 3, 4, (150, 200, 150)),
        ("3. Core Model Development", 3, 6, (204, 102, 0)),
        ("   Job Description Generator (LLM)", 3, 5, (230, 160, 100)),
        ("   CV Screening & Ranking Engine (NLP)", 4, 6, (230, 160, 100)),
        ("   Automated Job-Candidate Matching", 4, 6, (230, 160, 100)),
        ("   Intelligent Chatbot Assistant", 5, 6, (230, 160, 100)),
        ("   Skill Gap Analysis & Feedback Module", 5, 7, (230, 160, 100)),
        ("4. System Dev. & Integration", 5, 8, (170, 60, 170)),
        ("   UI/UX Design", 5, 6, (210, 150, 210)),
        ("   Full-Stack Development & API Integration", 6, 8, (210, 150, 210)),
        ("5. Evaluation & Testing", 8, 9, (110, 110, 110)),
        ("   System Testing & Performance Evaluation", 8, 9, (110, 110, 110)),
        ("6. Final Documentation & Closure", 9, 11, (170, 130, 20)),
        ("   Final Thesis & Submission", 9, 11, (170, 130, 20)),
    ]
    left = 380
    top = 60
    row_h = 20
    col_w = (W - left - 40) / len(months)
    f = load_font(11)
    for i, m in enumerate(months):
        dr.text((left + i*col_w + col_w/2 - 10, top - 20), m, font=f, fill=INK)
        dr.line([(left + i*col_w, top), (left + i*col_w, top + row_h*len(tasks))], fill=(220, 220, 220), width=1)
    for i, (name, s, e, color) in enumerate(tasks):
        y = top + i*row_h
        dr.text((10, y+2), name, font=load_font(10.5), fill=INK)
        dr.rectangle([left + s*col_w + 2, y+3, left + e*col_w - 2, y+row_h-3], fill=color)
    save(img, os.path.join(OUT, "project_timeline.png"))


if __name__ == "__main__":
    d_architecture()
    d_use_case()
    d_class_diagram()
    d_activity_diagram()
    d_sequence_matching()
    d_sequence_resume()
    d_sequence_evidence()
    d_dsrm_workflow()
    d_conceptual_map()
    d_rich_picture()
    d_timeline()
    print("ALL PIL DIAGRAMS DONE")
