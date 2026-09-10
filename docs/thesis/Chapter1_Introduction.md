# CHAPTER 01 — INTRODUCTION

## 1.1 Chapter Overview

This chapter introduces the research undertaken in the design and development of the **Intelligent Recruitment Automation System (IRAS)**, an AI-assisted recruitment platform that unifies candidate profiling, CV/resume intelligence, skill-taxonomy and semantic job matching, AI-generated skill assessments, personalized skill-gap closure, and role-aware conversational assistance within a single authenticated system. The chapter begins by describing the background against which the research problem emerged (Section 1.2), before formally stating the problem at both a general and a domain-specific level (Section 1.3). It then articulates the research question that the study sets out to answer (Section 1.4), the motivation behind pursuing it (Section 1.5), the overarching aim (Section 1.6), and the specific, measurable objectives derived from that aim (Section 1.7). A rich picture of the proposed solution is presented to visualize the actors, concerns, and workflows the system must reconcile (Section 1.8), followed by the hardware and software resources required to realize it (Section 1.9) and an explicit statement of what is, and is not, within the boundary of this research (Section 1.10). The chapter closes with a summary that bridges into Chapter 2 (Section 1.11).

## 1.2 Problem Background

Recruitment has always been a matching problem: employers hold a finite number of vacancies with specific skill requirements, and candidates hold a heterogeneous mix of skills, experience, and potential. Historically, this matching was performed manually — recruiters read paper or emailed CVs, shortlisted by hand, and interviewed. Over the last decade, the volume of applications flowing through digital channels has grown far faster than the human capacity to review them individually, and both employers and job platforms responded by adopting Applicant Tracking Systems (ATS) that filter, keyword-match, and rank incoming resumes automatically.

That shift solved a throughput problem but introduced a new one. Recent industry analysis of resume-screening behaviour reports that recruiters spend an average of only **7.4 seconds** scanning a resume before deciding whether to read further, and that **71.4%** of resumes are filtered out by ATS software before a human recruiter ever sees them (ResumeAdapter, 2026; OneHour Digital, 2026). At the same time, AI adoption inside recruiting workflows has accelerated sharply — the share of organizations using AI to support recruiting rose from roughly 26% in 2024 to 51% in 2025 (SHRM, 2025, as cited in RecruitAI Suite, 2026), and ATS software is now used by an estimated 78% of organizations, with the global AI-in-recruitment market projected to grow from USD 601.51 million in 2025 to over USD 1.15 billion by 2034 (Technavio, 2026). Recruitment, in other words, is already automated — but the automation in most widely deployed platforms is shallow: it filters and ranks by exact keyword overlap, without a taxonomy-aware understanding of skill synonyms, without a free-text semantic signal, and without any closed feedback loop that tells a rejected candidate "why" they were unsuccessful or "what" to do about it.

This shallow automation has a visible downstream effect in labour markets such as Sri Lanka's. Youth unemployment has been estimated at between 20% and 25% as of 2026, well above the general unemployment rate of around 4.3% (LankaNewsWeb, 2026), and multiple studies of Sri Lankan graduate employability attribute a significant share of this gap not to a shortage of vacancies but to a **skills mismatch** — graduates who are technically "employable" on paper are not perceived as job-ready by employers, and often have no structured, verifiable way to close that gap before or after applying (Dayaratna-Banda & Dharmadasa, 2022). The Tertiary and Vocational Education Commission's own 2025–2029 strategic plan explicitly targets this mismatch through competency-based, digitally assessed training (LankaNewsWeb, 2026), signalling that the problem is recognized at a policy level, not just anecdotally.

It is against this background — high-volume, low-signal, keyword-driven screening on the employer side, and opaque, feedback-free rejection with no structured path to improvement on the candidate side — that this research is positioned. The underlying question is not whether recruitment should be automated (it already is, almost everywhere), but whether it can be automated **intelligently and transparently enough** to genuinely improve the fit between candidates and jobs, rather than merely filtering faster.

## 1.3 Problem Statement

### 1.3.1 General Problem

At a high level, the conventional e-recruitment pipeline — whether a generic job board, a corporate careers page, or a commodity ATS — creates a one-directional, low-fidelity relationship between employers and job seekers. Employers receive a large, largely unranked or crudely keyword-ranked pool of applicants and must invest disproportionate time and cost to identify genuinely suitable candidates, while strong candidates are frequently screened out by rigid keyword filters that cannot recognize a skill expressed under a different name (ResumeAdapter, 2026). Candidates, in turn, receive little more than a binary outcome — shortlisted or not — with no explanation of "why", no quantified view of the gap between their current skill profile and the role's requirements, and no guided path to close that gap for the next opportunity. The consequence, at the level of the labour market as a whole, is a recruitment pipeline that is fast at rejecting but slow at developing: it filters people out efficiently while doing almost nothing to help the people it filters out become better matched candidates. This dynamic compounds a broader, well-documented graduate skills-mismatch problem in employment markets such as Sri Lanka's, where technically qualified graduates remain un- or under-employed because the gap between what they were taught and what employers need is never made explicit or actionable to them (Dayaratna-Banda & Dharmadasa, 2022).

### 1.3.2 Specific Problem

Within this general picture, existing recruitment platforms exhibit four concrete, technical shortcomings that this research treats as its point of departure:

1. **Shallow matching intelligence.** Conventional ATS filtering is exact-keyword or regular-expression based. It does not normalize skills against a taxonomy of aliases and synonyms, does not evaluate the free-text semantic fit between a resume and a job description, and does not combine multiple independent signals (skill overlap, semantic similarity, a learned candidate-job fit model, verified assessment performance) into a single, defensible score. As a result, ranking is either brittle — missing candidates whose CVs use different wording for the same skill — or entirely manual, which does not scale.

2. **No closed skill-gap loop.** Even where a platform does score a candidate against a role, it stops at the score. It does not decompose the gap into the specific missing or under-developed skills, does not explain that gap in a way a candidate can act on, does not generate a personalized, sequenced improvement plan toward those skills, and has no mechanism for a candidate to submit evidence of having closed the gap and have that evidence verified before it counts toward their profile.

3. **Fragmented, role-blind tooling.** Job description writing, applicant scoring and shortlisting, interview scheduling, skill-assessment design and grading, and day-to-day candidate/employer support are typically handled through disconnected tools — spreadsheets, generic email, separate SaaS products — rather than a single system that is aware of "who" is asking (a candidate, an employer, or a platform administrator) and can tailor its guidance, scoring, and conversational assistance to that role's authenticated context.

4. **Low auditability of AI-influenced decisions.** Where AI is already used in recruiting (job-description generation, resume screening, candidate communication — the most common applications reported in industry surveys; RecruitAI Suite, 2026), it is frequently deployed as an opaque filter. Neither the employer nor the candidate can see the breakdown behind an automated match score or an automated grade, which undermines trust in AI-assisted hiring at a time when adoption of exactly these tools is accelerating.

No single, freely adoptable platform currently integrates taxonomy-normalized skill matching, semantic similarity, a trained candidate-job fit classifier, AI-generated and AI-graded skill assessments, AI-explained skill gaps, an evidence-verified skill-improvement workflow, and an auditable, breakdown-visible scoring model — all within one authenticated, role-aware system that serves candidates, employers, and administrators alike. This is the research gap that the present study addresses.

## 1.4 Research Question

**How can an integrated, AI-assisted recruitment platform combine taxonomy-based skill matching, semantic similarity, and a trained candidate-job fit model with AI-generated skill-gap analysis and improvement planning to produce candidate-job matches that are simultaneously more accurate, more transparent, and more developmental than those produced by conventional keyword-based recruitment platforms?**

Within this primary question, the study is further guided by the following sub-questions:

- To what extent does combining skill-taxonomy matching, semantic similarity, and a trained fit classifier improve ranking quality over a single-signal (keyword-only) baseline?
- How can a rejected or partially matched candidate's skill gap be identified and explained in a way that is specific and actionable rather than generic?
- What system design allows AI-influenced decisions (matching, assessment grading, skill-plan generation) to remain auditable and explainable to the humans they affect?

## 1.5 Research Motivation

The motivation for this research is threefold. First, it is grounded in a **directly observed gap**: repeated exposure — as an applicant navigating job boards and as an observer of peers' job searches — to the experience of being rejected by keyword-driven filters with no explanation and no path forward, even when the underlying skill gap was small and closable. This is a familiar, frustrating experience for final-year undergraduates entering the job market, and it is one that existing platforms in the local market do almost nothing to address.

Second, it is grounded in a **technical opportunity**. The final-year curriculum brings together full-stack web development, applied machine learning, and natural-language processing — precisely the combination needed to move recruitment matching beyond keyword filters, by layering a trained fit-classifier and a semantic-similarity signal on top of a conventional skill-taxonomy match, and by using large language models to explain gaps, generate assessments, and hold role-aware conversations rather than only to generate boilerplate text.

Third, it is grounded in **social relevance**. The Sri Lankan graduate skills-mismatch problem is well documented (Dayaratna-Banda & Dharmadasa, 2022) and explicitly targeted by national policy (LankaNewsWeb, 2026), yet the tooling available to individual candidates to understand and close their own skill gaps remains limited. A system that makes a candidate's gap explicit, explains it in plain language, and gives them a verifiable plan to close it has a direct, measurable line to graduate employability — not just a more efficient hiring funnel for employers.

## 1.6 Research Aim

**To design, implement, and evaluate IRAS — an integrated, AI-assisted recruitment automation platform that improves the accuracy and transparency of candidate-job matching while giving candidates an explainable, evidence-verified pathway to close the skill gaps that separate them from the roles they want.**

## 1.7 Research Objectives

### 1.7.1 To Identify

- To identify the functional and non-functional shortcomings of existing recruitment and e-recruitment platforms through a review of academic literature, industry reports, and comparable systems.
- To identify the distinct information needs and concerns of the three stakeholder roles the system must serve — candidates, employers, and platform administrators.
- To identify the specific signals (skill-taxonomy overlap, semantic textual similarity, a learned fit probability, and verified assessment performance) that can be combined into a single, defensible candidate-job match score.

### 1.7.2 To Analyze

- To analyze existing skill-matching and resume-screening approaches (keyword/rule-based ATS filtering) against taxonomy-aware and semantic/ML-augmented alternatives, in order to establish the design rationale for IRAS's weighted, multi-signal scoring model.
- To analyze candidate resume data and job requirement data to design a skill taxonomy (with alias and synonym resolution) capable of normalizing free-text skill mentions into a consistent, matchable representation.
- To analyze the trade-offs between fully automated AI decisioning and human-in-the-loop review, particularly for skill-evidence verification and assessment grading, in order to determine where administrator oversight must remain in the workflow.

### 1.7.3 To Design / Implement / Develop

- To design and implement a relational data model and Web API (candidate, employer, admin domains) capable of supporting profile management, CV/resume parsing, job posting, application, and interview-scheduling workflows.
- To design and implement a multi-signal job-matching engine that combines skill-taxonomy matching, AI-computed semantic similarity, a trained candidate-job fit classifier, and skill-assessment performance into a single, configurable, auditable match score.
- To design and implement an AI-assisted skill-gap analysis and improvement-planning module that explains a candidate's gap against a target role, generates a personalized, sequenced improvement plan, and supports evidence submission with AI-assisted and administrator-reviewed verification.
- To design and implement an AI-assisted skill-assessment module capable of generating role-relevant questions and grading candidate responses.
- To design and implement a role-aware conversational assistant (chatbot) that tailors its responses to the authenticated context of a candidate, employer, or administrator.
- To design and implement administrative tooling for job moderation, audit logging, AI-service health monitoring, and platform reporting.

### 1.7.4 To Evaluate

- To evaluate the accuracy of the multi-signal matching model against a single-signal (keyword-only) baseline, using held-out candidate-job pairs.
- To evaluate the usability of the candidate, employer, and administrator experiences through structured user testing and feedback.
- To evaluate the reliability, response latency, and failure behaviour of the AI-dependent modules (resume parsing, ranking, skill-gap explanation, assessment grading, chat) under realistic and degraded (AI-service-unavailable) conditions.
- To evaluate the transparency of the system's AI-influenced decisions — i.e., whether candidates and employers can see and understand the basis of a match score, a skill-gap explanation, or an assessment grade.

## 1.8 Rich Picture of the Proposed Solution

The rich picture below (Figure 1.1) captures the actors, their individual concerns, and the principal information flows the proposed solution must reconcile. It is intended as an informal, Checkland-style rich picture rather than a formal process diagram, and should be read alongside the workflow narrative that follows.

**Actors and their concerns:**

- **Candidate** — "Why was I rejected, and what should I do about it?" Wants a CV that is understood correctly, jobs that are genuinely relevant, and a concrete plan to close any skill gap.
- **Employer** — "How do I find the right person without reading 300 CVs?" Wants a ranked, explainable shortlist and confidence that the ranking reflects real fit, not just keyword luck.
- **Platform Administrator** — "Is the AI behaving, and can I prove it?" Wants moderation control over job postings, an audit trail of AI-influenced decisions, and visibility into AI-service health.
- **AI/ML Engine** (Python microservice + Gemini-based generative components) — sits behind the platform, parsing resumes, computing semantic similarity and a trained fit score, explaining skill gaps, generating assessments and job descriptions, and powering the conversational assistant.
- **IRAS Core Platform** — the authenticated, role-aware system of record that orchestrates all of the above and is the single place candidates, employers, and administrators interact with.

**Principal workflow (see Figure 1.1):**

1. A **candidate** registers, builds a profile, and uploads a CV; the AI engine parses it, extracts skills against the taxonomy, and populates the candidate's structured profile.
2. An **employer** registers, creates an employer profile, and posts a job — optionally AI-assisted in drafting the job description — which is moderated by an **admin** before publication.
3. The **matching engine** computes a skill-taxonomy match, a semantic-similarity score, and a trained fit-classifier score for each eligible candidate-job pair, combines them into a single weighted match score, and proactively notifies candidates who clear the configured threshold.
4. A **candidate** applies to a role directly or via a proactive match; the **employer** reviews an explainable, ranked applicant list broken down by skill match, experience, education, resume relevance, and (where set) assessment performance.
5. Where a gap exists between a candidate's profile and a role, the system explains the gap in plain language and generates a personalized **skill improvement plan**; the candidate works through it and submits **evidence**, which is reviewed by the AI engine and, where required, an **admin**, before the candidate's profile is updated.
6. An **employer** may attach a **skill assessment** to a role; the AI engine generates questions, the candidate attempts it, and the AI engine grades the response.
7. All three roles can converse with a **role-aware chatbot** that answers only within the scope of what that authenticated role is entitled to see and do.
8. The **admin** moderates jobs, reviews the audit log of AI-influenced actions, and monitors the AI service's health and availability.

**Figure 1.1 — Rich Picture of the Proposed IRAS Solution.** An SVG diagram implementing this workflow has been generated as a companion figure at `docs/thesis/figure-1-1-rich-picture.svg`. Insert it here as Figure 1.1 when assembling the final thesis document.

## 1.9 Resource Requirements

### 1.9.1 Hardware

- **Development workstation** — multi-core CPU (Intel i5 / AMD Ryzen 5 or better, 8-core recommended for concurrent API + database + AI-service + frontend workloads).
- **Memory** — 16 GB RAM minimum; 32 GB recommended when running the Python AI service and the frontend build alongside the .NET API and PostgreSQL locally.
- **Storage** — 256 GB+ SSD (fast I/O for database, Docker images, and NuGet/npm/pip package caches).
- **Network** — a stable broadband connection (calls to the hosted Gemini API and Supabase-hosted database/storage).
- **Deployment infrastructure** — no dedicated on-premises server hardware is required; the platform is deployed to managed cloud infrastructure (see Section 1.9.2).
- **Client (end-user) devices** — any device with a modern web browser; the platform is delivered as a responsive web application, not a native mobile app.

### 1.9.2 Software

- **Backend API** — ASP.NET Core Web API (.NET 10), organized as Domain / Application / Infrastructure / API projects.
- **Data access** — Entity Framework Core with the Npgsql provider.
- **Database** — PostgreSQL, managed via Supabase.
- **File/object storage** — Supabase Storage, with a local-disk provider for development.
- **Authentication** — JWT Bearer authentication with role-based authorization (Candidate / Employer / Admin).
- **AI/ML microservice** — a companion Python service (FastAPI, served via Uvicorn) exposing resume parsing, candidate ranking (semantic similarity + trained fit-classifier), and a health-check endpoint.
- **Generative AI** — the Google Gemini API, used for job-description generation, skill-gap explanation, skill-improvement-plan generation, assessment question generation and grading, feedback generation, and the conversational assistant.
- **Frontend** — a Vite-based single-page application (React/TypeScript) providing separate candidate, employer, and admin experiences, consuming the Web API.
- **Containerization & deployment** — Docker (multi-stage build) deployed to Render.com.
- **Testing** — xUnit for backend unit/integration tests; Postman/Swagger (OpenAPI) for manual and exploratory API testing.
- **Version control & CI** — Git and GitHub.
- **Tooling** — Visual Studio / VS Code, pgAdmin or a Supabase dashboard for database inspection, and a diagramming tool (e.g., draw.io) for system diagrams.

Note: the exact Python NLP/ML library set (e.g., the embedding model and classifier implementation used by the AI microservice) should be confirmed against the AI-service repository and inserted here before final submission, as that service is maintained outside this backend repository.

## 1.10 Project Scope

**In scope:**

- Candidate registration, profile management, CV upload and AI-based parsing, and an integrated CV builder.
- Employer registration, profile management, job posting, and AI-assisted job-description generation.
- Multi-signal job matching (skill-taxonomy match, semantic similarity, trained ML fit-classifier, assessment score) with a configurable, auditable scoring formula.
- Proactive and reactive candidate-job matching with in-app notifications.
- AI-explained skill-gap analysis and personalized skill-improvement plans.
- Evidence submission for skill improvement, with AI-assisted and administrator-reviewed verification.
- AI-generated and AI-graded skill assessments attached to job postings.
- Job application lifecycle management and interview scheduling.
- A role-aware conversational assistant for candidates, employers, and administrators.
- Administrative job moderation, audit logging, user management, AI-service health monitoring, and platform reporting.
- Role-based authentication and authorization (Candidate, Employer, Admin).

**Out of scope:**

- Native mobile applications (Android/iOS) — delivery is a responsive web application only.
- Post-hire HR processes: payroll, onboarding, performance management.
- Live video interviewing or interview proctoring — the system supports scheduling only, not conducting, interviews.
- Payment processing, subscription billing, or premium-tier monetization.
- Integration with or import from third-party job boards (e.g., LinkedIn).
- Multi-language localization (the platform is developed and evaluated in English).
- Formal background/criminal-record verification of candidates.
- Offline/disconnected operation.
- Real-time video/voice chat between candidates and employers.

## 1.11 Chapter Summary

This chapter established the context and justification for the research: a recruitment landscape in which automation has already arrived but remains shallow — fast at filtering, slow at explaining and developing — and a Sri Lankan labour-market backdrop in which graduate skills mismatch is a documented, policy-level concern rather than an abstract inconvenience. From this background, the chapter derived a general problem (a one-directional, low-fidelity recruitment pipeline) and a specific, technical problem statement (shallow matching, no closed skill-gap loop, fragmented role-blind tooling, and low auditability of AI decisions), and used that gap to motivate a single, focused research question. The research aim and objectives translate that question into what the study will build — IRAS, an integrated matching, skill-gap, assessment, and conversational-assistance platform — and what it will measure. The rich picture visualized how candidates, employers, administrators, and the underlying AI engine interact through that platform, and the resource-requirement and scope sections fixed the boundary of what this research does, and deliberately does not, attempt to solve. Chapter 2 builds on this foundation with a critical review of existing literature and comparable systems.

---

### References Cited in This Chapter

- Dayaratna-Banda, O. G., & Dharmadasa, P. D. C. S. (2022). An Economics Analysis of Employability and Unemployment of Humanities and Social Sciences Graduates in Sri Lanka. South Asian Survey, 29(2), 155–180. https://journals.sagepub.com/doi/abs/10.1177/09715231221124714
- LankaNewsWeb. (2026). Sri Lanka's Educated Youth Trapped in Skills Mismatch Crisis. https://lankanewsweb.net/archives/197586/sri-lankas-educated-youth-trapped-in-skills-mismatch-crisis/
- OneHour Digital. (2026). Recruiter Screening Behavior Statistics for 2026. https://onehour.digital/blog/recruiter-screening-behavior-statistics
- RecruitAI Suite. (2026). 47 AI Recruiting Statistics for 2026 (SHRM & LinkedIn Data). https://copilot.recruitaisuite.com/blog/ai-recruiting-statistics-2026/
- ResumeAdapter. (2026). The 2026 ATS Rejection Report: What 10,000 Resume Scans Reveal About Why Qualified Candidates Get Rejected. https://www.resumeadapter.com/blog/2026-ats-rejection-report-10000-resume-scans
- Technavio. (2026). AI Market Industry in Recruitment Industry Analysis, 2026–2030. https://www.technavio.com/report/ai-market-industry-in-recruitment-industry-analysis

Note: several of the statistics above are drawn from industry/trade sources (recruitment blogs, market-research vendors) rather than peer-reviewed journals. This is acceptable for motivating a problem statement, but before final submission you should cross-check the Sri Lanka-specific unemployment figures against a primary source — the Department of Census and Statistics (Sri Lanka) Labour Force Survey or the Central Bank of Sri Lanka's Annual Report — and cite the primary source directly wherever possible, as examiners typically weight those more heavily than secondary blog citations.
