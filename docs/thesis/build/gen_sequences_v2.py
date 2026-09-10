"""
Complete set of sequence diagrams for IRAS — one per major system workflow,
covering every actor (Candidate, Employer, Admin) and the AI service.
Regenerates the 3 existing diagrams (unchanged content, consistent styling)
plus 7 new ones so the full request/response lifecycle of the system is
documented, not just the matching/parsing/evidence paths.
"""
import os
from diagram_helpers import load_font, arrow, label, box, save, INK, ACCENT, WHITE

OUT = os.path.join(os.path.dirname(__file__), "figures")


def _seq_base(W, H, title, lifelines):
    from PIL import Image, ImageDraw
    img = Image.new("RGB", (W, H), WHITE)
    dr = ImageDraw.Draw(img)
    f_title = load_font(19, bold=True)
    dr.text((W/2 - dr.textlength(title, font=f_title)/2, 10), title, font=f_title, fill=INK)
    n = len(lifelines)
    margin = 95
    usable = W - 2 * margin
    xs = [margin + (usable * i / (n - 1) if n > 1 else 0) for i in range(n)]
    top = 58
    for x, name in zip(xs, lifelines):
        box(dr, x - 88, top, 176, 42, title=name, title_font=load_font(12, bold=True))
        dr.line([(x, top + 42), (x, H - 26)], fill=(140, 140, 140), width=1)
    return img, dr, xs, top + 42


def steps(dr, xs, y0, items, row_h=42):
    """items: list of (from_idx, to_idx, label, is_return) OR ('frame_start', text) /
    ('frame_mid', text) / ('frame_end',) for an alt/opt combined-fragment box."""
    y = y0 + 26
    frame_stack = []
    for it in items:
        if it[0] == "frame_start":
            _, text, span = it
            fx0 = xs[min(span)] - 40
            fx1 = xs[max(span)] + 40
            frame_stack.append((fx0, fx1, y))
            dr.text((fx0 + 6, y + 4), f"alt  {text}", font=load_font(10.5, bold=True), fill=(90, 60, 150))
            y += 34
            continue
        if it[0] == "frame_mid":
            fx0, fx1, _ = frame_stack[-1]
            dr.line([(fx0, y), (fx1, y)], fill=(120, 120, 140), width=1)
            dr.text((fx0 + 6, y + 4), f"[{it[1]}]", font=load_font(10), fill=(90, 60, 150))
            y += 32
            continue
        if it[0] == "frame_end":
            fx0, fx1, fy0 = frame_stack.pop()
            dr.rectangle([fx0, fy0 - 4, fx1, y - 6], outline=(120, 120, 140), width=2)
            y += 22
            continue
        a, b, text, ret = it
        color = (120, 120, 120) if ret else ACCENT
        if a == b:
            # self-call
            x = xs[a]
            dr.line([(x, y), (x + 60, y), (x + 60, y + 22), (x, y + 22)], fill=color, width=2)
            arrow_head(dr, (x, y + 22), color)
            label(dr, x + 90, y + 4, text, font=load_font(9.5))
            y += 52
        else:
            arrow(dr, (xs[a], y), (xs[b], y), color=color, width=2, dashed=ret)
            mx = (xs[a] + xs[b]) / 2
            label(dr, mx, y - 12, text, font=load_font(9.5))
            y += row_h
    return y


def arrow_head(dr, tip, color):
    import math
    x, y = tip
    dr.line([(x, y), (x + 8, y - 4)], fill=color, width=2)
    dr.line([(x, y), (x + 8, y + 4)], fill=color, width=2)


# ------------------------------------------------------------------ 1
def seq_registration():
    life = ["User (Candidate/Employer/Admin)", "AuthController", "UserService", "Database"]
    img, dr, xs, y0 = _seq_base(1350, 700, "Sequence Diagram — Registration & Authentication", life)
    items = [
        (0, 1, "POST /auth/register (email, password, role)", False),
        (1, 2, "RegisterAsync(dto)", False),
        (2, 3, "INSERT User (PasswordHash, Role, AuthProvider)", False),
        (3, 2, "OK", True),
        (2, 1, "UserDto", True),
        (1, 0, "201 Created", True),
        (0, 1, "POST /auth/login (email, password)", False),
        (1, 2, "ValidateCredentialsAsync(email, password)", False),
        (2, 3, "SELECT User WHERE Email = ...", False),
        (3, 2, "User row (PasswordHash, Role)", True),
        (2, 1, "user (verified)", True),
        (1, 1, "GenerateJwt(sub, role claim)", False),
        (1, 0, "200 OK { token }", True),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_registration.png"))


# ------------------------------------------------------------------ 2
def seq_jd_generation():
    life = ["Employer", "EmployerJobsController", "JobService / GeminiJdGenerator", "Administrator", "Database"]
    img, dr, xs, y0 = _seq_base(1550, 850, "Sequence Diagram — AI-Assisted Job Description, Moderation & Publishing", life)
    items = [
        (0, 1, "POST /employer/jobs/generate-description", False),
        (1, 2, "GenerateJobDescriptionAsync(request)", False),
        (2, 2, "call Gemini LLM (title, skills, seniority)", False),
        (2, 1, "draft JD text (editable, requiresApproval=true)", True),
        (1, 0, "200 OK (draft)", True),
        (0, 1, "POST /employer/jobs (approved JD)", False),
        (1, 4, "INSERT Job (Status = Draft)", False),
        (4, 1, "OK", True),
        (1, 0, "201 Created (pending moderation)", True),
        (3, 1, "GET /admin/jobs?status=Draft", False),
        (1, 4, "SELECT Job WHERE Status = Draft", False),
        (4, 1, "rows", True),
        (1, 3, "job list", True),
        (3, 1, "PUT /admin/jobs/{id}/approve", False),
        (1, 4, "UPDATE Job SET Status=Published, PostedAt=now", False),
        (4, 1, "OK", True),
        (1, 3, "200 OK", True),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_jd_generation.png"))


# ------------------------------------------------------------------ 3
def seq_proactive_matching():
    life = ["Publish Trigger", "JobMatchingService", "ScoringService", "AI Service (/rank)", "Database", "NotificationService"]
    img, dr, xs, y0 = _seq_base(1650, 680, "Sequence Diagram — Proactive Batch Job Matching & Notification", life)
    items = [
        (0, 1, "RunMatchingForJobAsync(jobId)", False),
        (1, 4, "SELECT eligible CandidateProfiles (OptInMatching, has resume)", False),
        (4, 1, "candidates[]", True),
        (1, 2, "ComputeMatchSignalsAsync(job, candidates)", False),
        (2, 3, "POST /rank (batched — one call for all candidates)", False),
        (3, 2, "semanticSimilarity, fitScore per candidate", True),
        (2, 1, "MatchSignals dictionary", True),
        (1, 1, "ComputeSkillMatch + ComputeTotalScore per candidate", False),
        (1, 4, "INSERT JobMatch rows (score, thresholdPassed)", False),
        (4, 1, "OK", True),
        (1, 5, "NotifyAsync(candidateId, JobMatch) [passed only]", False),
        (5, 4, "INSERT Notification", False),
        (4, 5, "OK", True),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_proactive_matching.png"))


# ------------------------------------------------------------------ 4
def seq_skill_gap():
    life = ["Candidate", "SkillGapsController", "SkillGapService / Gemini Explainer", "SkillImprovementPlanService / Gemini Planner", "Database"]
    img, dr, xs, y0 = _seq_base(1650, 740, "Sequence Diagram — Skill Gap Explanation & Improvement Plan Generation", life)
    items = [
        (0, 1, "GET /skill-gaps/{jobId}", False),
        (1, 2, "AnalyzeGapAsync(candidateId, jobId)", False),
        (2, 4, "compare CandidateSkills vs JobRequiredSkills", False),
        (4, 2, "missing skills[]", True),
        (2, 2, "Explain(missingSkills, job) via Gemini", False),
        (2, 1, "SkillGapDto[] (with plain-language explanation)", True),
        (1, 0, "200 OK", True),
        (0, 1, "POST /skill-plans (skillId)", False),
        (1, 3, "GeneratePlanAsync(candidateId, skillId)", False),
        (3, 3, "Generate(skill, candidateContext) via Gemini", False),
        (3, 4, "INSERT SkillImprovementPlan + SkillPlanStep rows", False),
        (4, 3, "OK", True),
        (3, 0, "201 Created (plan + steps)", True),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_skill_gap.png"))


# ------------------------------------------------------------------ 5
def seq_assessment():
    life = ["Candidate", "AssessmentsController", "AssessmentService", "Gemini Q-Gen / Grader", "Database"]
    img, dr, xs, y0 = _seq_base(1550, 900, "Sequence Diagram — Skill Assessment: Generation, Attempt & AI Grading", life)
    items = [
        (0, 1, "POST /assessments/{jobId}/start", False),
        (1, 2, "StartAttemptAsync(candidateId, jobId)", False),
        (2, 4, "SELECT JobAssessment WHERE JobId", False),
        (4, 2, "(none found)", True),
        ("frame_start", "assessment not yet generated for this job", (2, 3)),
        (2, 3, "GenerateQuestions(job.RequiredSkills)", False),
        (3, 2, "questions[] (MCQ + FreeText)", True),
        (2, 4, "INSERT JobAssessment + AssessmentQuestion rows", False),
        ("frame_end",),
        (2, 4, "INSERT CandidateAssessmentAttempt (InProgress)", False),
        (4, 2, "attemptId", True),
        (2, 0, "200 OK (questions, no correct answers)", True),
        (0, 1, "POST /assessments/attempts/{id}/submit (answers[])", False),
        (1, 2, "SubmitAsync(attemptId, answers)", False),
        (2, 4, "INSERT CandidateAssessmentAnswer (MCQ scored locally)", False),
        (2, 3, "GradeFreeText(answer, modelAnswer) [FreeText only]", False),
        (3, 2, "scoreFraction per answer", True),
        (2, 4, "UPDATE Attempt SET Score, Status = Completed", False),
        (4, 2, "OK", True),
        (2, 0, "200 OK (AssessmentResultDto: Score)", True),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_assessment.png"))


# ------------------------------------------------------------------ 6
def seq_interview_feedback():
    life = ["Employer", "EmployerApplicationsController", "InterviewService / FeedbackService", "Gemini Feedback Gen.", "Database", "Candidate"]
    img, dr, xs, y0 = _seq_base(1650, 900, "Sequence Diagram — Interview Scheduling & Candidate Feedback", life)
    items = [
        (0, 1, "PUT /applications/{id}/status (Shortlisted)", False),
        (1, 4, "UPDATE Application; INSERT ApplicationStatusHistory", False),
        (4, 1, "OK", True),
        (0, 1, "POST /applications/{id}/interview", False),
        (1, 2, "ScheduleAsync(applicationId, dto)", False),
        (2, 4, "INSERT Interview (Status = Scheduled)", False),
        (4, 2, "OK", True),
        (2, 0, "201 Created (InterviewDto)", True),
        (0, 1, "POST /applications/{id}/feedback (decision)", False),
        (1, 2, "GenerateDraftAsync(applicationId)", False),
        (2, 3, "Generate(application, decision) via Gemini", False),
        (3, 2, "draft message text", True),
        (2, 0, "draft (ApprovalStatus = PendingReview)", True),
        (0, 1, "PUT /feedback/{id}/approve", False),
        (1, 2, "ApproveAndSendAsync(feedbackId)", False),
        (2, 4, "UPDATE Feedback SET ApprovalStatus=Approved, DeliveryStatus=Sent", False),
        (4, 2, "OK", True),
        (2, 5, "Notification: your application has feedback", False),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_interview_feedback.png"))


# ------------------------------------------------------------------ 7
def seq_chatbot():
    life = ["User (any role)", "ChatController", "ChatService", "Role ContextBuilder", "Gemini + ChatScopeGate", "Database"]
    img, dr, xs, y0 = _seq_base(1650, 920, "Sequence Diagram — Role-Aware Chatbot Interaction", life)
    items = [
        (0, 1, "POST /chat/messages (text)", False),
        (1, 2, "SendMessageAsync(userId, text)", False),
        (2, 2, "SelectContextBuilder(user.Role)", False),
        (2, 3, "Build(user)  — only this role's own data", False),
        (3, 5, "SELECT role-scoped profile/job/admin data", False),
        (5, 3, "rows", True),
        (3, 2, "ChatContext", True),
        (2, 4, "Respond(context, message) via Gemini", False),
        (4, 2, "reply text", True),
        (2, 4, "IsInScope(reply)", False),
        ("frame_start", "reply scope check", (2, 4)),
        (4, 2, "false — out of scope", True),
        ("frame_mid", "out of scope"),
        (2, 0, "fallback: \"I can only help with recruitment.\"", True),
        ("frame_mid", "in scope"),
        (2, 5, "INSERT ChatMessage (User + Bot)", False),
        (5, 2, "OK", True),
        (2, 0, "200 OK (reply)", True),
        ("frame_end",),
    ]
    steps(dr, xs, y0, items)
    save(img, os.path.join(OUT, "sequence_chatbot.png"))


if __name__ == "__main__":
    seq_registration()
    seq_jd_generation()
    seq_proactive_matching()
    seq_skill_gap()
    seq_assessment()
    seq_interview_feedback()
    seq_chatbot()
    print("7 new sequence diagrams done")
