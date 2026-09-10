"""
Assembles the full IRAS thesis (.docx) from the plain-text content files in
content/ and the generated figures in figures/, applying the required
formatting: Times New Roman throughout; Title 14pt; H1 (numbered sections)
14pt bold black; H2 (numbered sub-sections) 12pt bold black; figure captions
bold 8pt with a minimum two-sentence description.
"""
import os
import re
from docx import Document
from docx.shared import Pt, Inches, RGBColor, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement

ROOT = os.path.dirname(__file__)
CONTENT = os.path.join(ROOT, "content")
FIGURES = os.path.join(ROOT, "figures")
OUT_PATH = os.path.abspath(os.path.join(ROOT, "..", "IRAS_Full_Thesis.docx"))

FONT = "Times New Roman"
BLACK = RGBColor(0, 0, 0)
LINK_BLUE = RGBColor(0, 0, 238)
FIG_WIDTH_IN = 6.0  # usable width is 15.5cm/6.10in (21cm page - 3.0cm - 2.5cm margins); 6.0in stays safely inside it

_bookmark_id = [100]


def _next_bookmark_id():
    _bookmark_id[0] += 1
    return _bookmark_id[0]


def add_bookmark(paragraph, name):
    bm_id = str(_next_bookmark_id())
    start = OxmlElement('w:bookmarkStart')
    start.set(qn('w:id'), bm_id)
    start.set(qn('w:name'), name)
    end = OxmlElement('w:bookmarkEnd')
    end.set(qn('w:id'), bm_id)
    paragraph._p.insert(0, start)
    paragraph._p.append(end)


def bookmark_name(label):
    return re.sub(r'[^A-Za-z0-9_]', '_', label)[:40]


def add_hyperlink_run(paragraph, anchor, text, size=11, bold=False):
    hyperlink = OxmlElement('w:hyperlink')
    hyperlink.set(qn('w:anchor'), anchor)
    new_run = OxmlElement('w:r')
    rPr = OxmlElement('w:rPr')
    rFonts = OxmlElement('w:rFonts')
    rFonts.set(qn('w:ascii'), FONT); rFonts.set(qn('w:hAnsi'), FONT); rFonts.set(qn('w:eastAsia'), FONT)
    rPr.append(rFonts)
    sz = OxmlElement('w:sz'); sz.set(qn('w:val'), str(int(size * 2))); rPr.append(sz)
    if bold:
        rPr.append(OxmlElement('w:b'))
    color_el = OxmlElement('w:color'); color_el.set(qn('w:val'), '0000EE'); rPr.append(color_el)
    u = OxmlElement('w:u'); u.set(qn('w:val'), 'single'); rPr.append(u)
    new_run.append(rPr)
    t = OxmlElement('w:t'); t.text = text; t.set(qn('xml:space'), 'preserve')
    new_run.append(t)
    hyperlink.append(new_run)
    paragraph._p.append(hyperlink)

CHAPTER_FILES = [
    "ch1_introduction.txt",
    "ch2_literature_review.txt",
    "ch3_methodology.txt",
    "ch4_srs.txt",
    "ch5_implementation.txt",
    "ch6_testing.txt",
    "ch7_conclusion.txt",
]


class Tracker:
    def __init__(self):
        self.figures = []  # (label, caption)
        self.tables = []   # (label, caption)
        self.word_count = 0


# ---------------------------------------------------------------- utilities
def set_run_font(run, size=12, bold=False, italic=False, color=BLACK, name=FONT, mono=False):
    run.font.name = name if not mono else "Consolas"
    run.font.size = Pt(size)
    run.font.bold = bold
    run.font.italic = italic
    run.font.color.rgb = color
    rPr = run._element.get_or_add_rPr()
    rFonts = rPr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = OxmlElement('w:rFonts')
        rPr.append(rFonts)
    rFonts.set(qn('w:eastAsia'), name if not mono else "Consolas")


def add_page_break(doc):
    doc.add_page_break()


def setup_heading_styles(doc):
    """Redefine Word's built-in Heading 1/2/3 styles to the required look
    (chapter titles + '1st headings' = 14pt bold black; 'sub-section' =
    12pt bold black) while keeping them genuine Word heading styles, so the
    TOC \\o "1-3" \\h field can enumerate AND hyperlink every one of them —
    plain direct-formatted paragraphs cannot be found by a TOC field."""
    h1 = doc.styles['Heading 1']
    h1.font.name = FONT
    h1.font.size = Pt(14)
    h1.font.bold = True
    h1.font.color.rgb = BLACK
    h1.font.italic = False
    h1.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    h1.paragraph_format.space_before = Pt(6)
    h1.paragraph_format.space_after = Pt(18)
    h1.paragraph_format.keep_with_next = True
    _set_east_asian_font(h1.font)

    h2 = doc.styles['Heading 2']
    h2.font.name = FONT
    h2.font.size = Pt(14)
    h2.font.bold = True
    h2.font.color.rgb = BLACK
    h2.font.italic = False
    h2.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.LEFT
    h2.paragraph_format.space_before = Pt(16)
    h2.paragraph_format.space_after = Pt(8)
    h2.paragraph_format.keep_with_next = True
    _set_east_asian_font(h2.font)

    h3 = doc.styles['Heading 3']
    h3.font.name = FONT
    h3.font.size = Pt(12)
    h3.font.bold = True
    h3.font.color.rgb = BLACK
    h3.font.italic = False
    h3.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.LEFT
    h3.paragraph_format.space_before = Pt(12)
    h3.paragraph_format.space_after = Pt(6)
    h3.paragraph_format.keep_with_next = True
    _set_east_asian_font(h3.font)


def _set_east_asian_font(font):
    rpr = font.element.get_or_add_rPr()
    rFonts = rpr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = OxmlElement('w:rFonts')
        rpr.append(rFonts)
    rFonts.set(qn('w:eastAsia'), FONT)


def add_chapter_title(doc, text, new_page=True):
    if new_page:
        add_page_break(doc)
    p = doc.add_paragraph(text, style='Heading 1')
    add_bookmark(p, bookmark_name("Chapter_" + text[:30]))
    return p


def add_h1(doc, text):
    """'1st headings' per the formatting spec — numbered sections like '4.2 Stakeholder Analysis' — mapped to Word's Heading 2 so the TOC field's outline levels 1-3 line up with Chapter/Section/Subsection."""
    p = doc.add_paragraph(text, style='Heading 2')
    return p


def add_h2(doc, text):
    """'Sub section' per the formatting spec — e.g. '4.4.1 Use Case Diagrams' — mapped to Word's Heading 3."""
    p = doc.add_paragraph(text, style='Heading 3')
    return p


INLINE_TOKEN = re.compile(r"\*\*(.+?)\*\*|_(.+?)_")


def add_inline_runs(paragraph, text, size=12, base_bold=False):
    """Renders **bold** and _italic_ spans (IEEE references use italics for
    the journal/conference/book title) within a single paragraph."""
    pos = 0
    for m in INLINE_TOKEN.finditer(text):
        if m.start() > pos:
            r = paragraph.add_run(text[pos:m.start()])
            set_run_font(r, size=size, bold=base_bold)
        if m.group(1) is not None:
            r = paragraph.add_run(m.group(1))
            set_run_font(r, size=size, bold=True)
        else:
            r = paragraph.add_run(m.group(2))
            set_run_font(r, size=size, bold=base_bold, italic=True)
        pos = m.end()
    if pos < len(text):
        r = paragraph.add_run(text[pos:])
        set_run_font(r, size=size, bold=base_bold)


def add_body_paragraph(doc, text, tracker):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    p.paragraph_format.space_after = Pt(10)
    p.paragraph_format.line_spacing = 1.4
    add_inline_runs(p, text, size=12)
    tracker.word_count += len(text.split())
    return p


def add_bullet(doc, text, tracker):
    p = doc.add_paragraph(style="List Bullet")
    p.paragraph_format.space_after = Pt(4)
    add_inline_runs(p, text, size=12)
    tracker.word_count += len(text.split())
    return p


def add_numbered(doc, text, tracker):
    p = doc.add_paragraph(style="List Number")
    p.paragraph_format.space_after = Pt(4)
    add_inline_runs(p, text, size=12)
    tracker.word_count += len(text.split())
    return p


def add_table_caption(doc, label, caption):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(10)
    p.paragraph_format.space_after = Pt(4)
    add_bookmark(p, bookmark_name(label))
    r = p.add_run(f"{label}: {caption}")
    set_run_font(r, size=8, bold=True)
    return p


def add_figure_caption(doc, label, caption):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(14)
    add_bookmark(p, bookmark_name(label))
    r = p.add_run(f"{label}: {caption}")
    set_run_font(r, size=8, bold=True)
    return p


def set_cell_text(cell, text, bold=False, size=10.5):
    cell.text = ""
    p = cell.paragraphs[0]
    r = p.add_run(text)
    set_run_font(r, size=size, bold=bold)


def style_table_borders(table):
    tbl = table._tbl
    tblPr = tbl.tblPr
    borders = OxmlElement('w:tblBorders')
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement(f'w:{edge}')
        el.set(qn('w:val'), 'single')
        el.set(qn('w:sz'), '4')
        el.set(qn('w:space'), '0')
        el.set(qn('w:color'), '808080')
        borders.append(el)
    tblPr.append(borders)


def add_table(doc, rows, tracker):
    parsed = [[c.strip() for c in r.strip().strip('|').split('|')] for r in rows]
    ncols = max(len(r) for r in parsed)
    table = doc.add_table(rows=len(parsed), cols=ncols)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    style_table_borders(table)
    for ri, row in enumerate(parsed):
        for ci in range(ncols):
            text = row[ci] if ci < len(row) else ""
            set_cell_text(table.cell(ri, ci), text, bold=(ri == 0))
            tracker.word_count += len(text.split())
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def add_figure(doc, spec, tracker, missing_ok=False):
    path, label, caption = spec.split("|", 2)
    label, caption, path = label.strip(), caption.strip(), path.strip()
    full = os.path.join(FIGURES, path)
    if missing_ok and not os.path.exists(full):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(f"[ PENDING — insert {path} here once supplied ]")
        set_run_font(r, size=11, italic=True, color=RGBColor(160, 30, 30))
    else:
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = p.add_run()
        run.add_picture(full, width=Inches(FIG_WIDTH_IN))
    add_figure_caption(doc, label, caption)
    tracker.figures.append((label, caption))
    tracker.word_count += len(caption.split())


def add_code(doc, code_text):
    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(10)
    p.paragraph_format.left_indent = Inches(0.3)
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:fill'), 'F3F3F3')
    p._p.get_or_add_pPr().append(shd)
    lines = code_text.split("\n")
    for i, line in enumerate(lines):
        r = p.add_run(line.replace("\t", "    "))
        set_run_font(r, size=10, mono=True)
        if i < len(lines) - 1:
            r.add_break()


# ------------------------------------------------------------- main parser
def render_markup(doc, text, tracker, chapter_mode=False):
    lines = text.split("\n")
    i, n = 0, len(lines)
    buf = []

    def flush():
        if buf:
            add_body_paragraph(doc, " ".join(buf).strip(), tracker)
            buf.clear()

    while i < n:
        raw = lines[i]
        s = raw.strip()
        if s == "":
            flush(); i += 1; continue
        if s.startswith("#C "):
            flush(); add_chapter_title(doc, s[3:]); i += 1; continue
        if s.startswith("### "):
            flush(); add_h2(doc, s[4:]); i += 1; continue
        if s.startswith("## "):
            flush(); add_h2(doc, s[3:]); i += 1; continue
        if s.startswith("# "):
            flush(); add_h1(doc, s[2:]); i += 1; continue
        if s.startswith("- "):
            flush(); add_bullet(doc, s[2:], tracker); i += 1; continue
        if re.match(r"^\d+\)\s", s):
            flush(); add_numbered(doc, re.sub(r"^\d+\)\s", "", s), tracker); i += 1; continue
        if s.startswith("TCAP:"):
            flush()
            label, caption = s[5:].split("|", 1)
            add_table_caption(doc, label.strip(), caption.strip())
            tracker.tables.append((label.strip(), caption.strip()))
            i += 1; continue
        if s.startswith("|"):
            flush()
            trows = []
            while i < n and lines[i].strip().startswith("|"):
                trows.append(lines[i].strip()); i += 1
            add_table(doc, trows, tracker)
            continue
        if s.startswith("FIGWIDE:"):
            # No landscape pages in this document — a wide diagram is still
            # inserted portrait, at the standard safe width; it simply flows
            # across as many pages as it needs, same as an oversized table.
            flush(); add_figure(doc, s[8:], tracker); i += 1; continue
        if s.startswith("FIGPEND:"):
            # A figure the author has not supplied yet (e.g. a UI screenshot
            # or an externally-produced chart) — renders as a clearly marked
            # placeholder instead of crashing the build.
            flush(); add_figure(doc, s[8:], tracker, missing_ok=True); i += 1; continue
        if s.startswith("FIG:"):
            flush(); add_figure(doc, s[4:], tracker); i += 1; continue
        if s == "CODE:":
            flush(); i += 1
            code_lines = []
            while i < n and lines[i].strip() != "ENDCODE":
                code_lines.append(lines[i]); i += 1
            i += 1
            add_code(doc, "\n".join(code_lines))
            continue
        buf.append(s); i += 1
    flush()


# ------------------------------------------------------------ front matter
def set_default_style(doc):
    style = doc.styles["Normal"]
    style.font.name = FONT
    style.font.size = Pt(12)
    style.paragraph_format.line_spacing = 1.4
    rpr = style.element.get_or_add_rPr()
    rFonts = rpr.find(qn('w:rFonts'))
    if rFonts is None:
        rFonts = OxmlElement('w:rFonts')
        rpr.append(rFonts)
    rFonts.set(qn('w:eastAsia'), FONT)
    sec = doc.sections[0]
    sec.page_height = Cm(29.7)
    sec.page_width = Cm(21.0)
    sec.top_margin = Cm(2.5)
    sec.bottom_margin = Cm(2.5)
    sec.left_margin = Cm(3.0)
    sec.right_margin = Cm(2.5)


def add_footer_page_numbers(doc):
    section = doc.sections[0]
    footer = section.footer
    p = footer.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = p.add_run()
    fld1 = OxmlElement('w:fldSimple')
    fld1.set(qn('w:instr'), 'PAGE')
    run._r.addnext(fld1)
    set_run_font(run, size=10)


def add_toc_field(doc):
    p = doc.add_paragraph()
    run = p.add_run()
    fld = OxmlElement('w:fldSimple')
    fld.set(qn('w:instr'), 'TOC \\o "1-3" \\h \\z \\u')
    r_el = OxmlElement('w:r')
    t_el = OxmlElement('w:t')
    t_el.text = "Right-click here and choose \"Update Field\" (or press F9) to generate the Table of Contents."
    r_el.append(t_el)
    fld.append(r_el)
    p._p.append(fld)


def title_page(doc):
    title_lines = [
        "INTELLIGENT RECRUITMENT AUTOMATION SYSTEM",
        "FOR JOB DESCRIPTION CREATION, APPLICATION SHORTLISTING,",
        "AND SKILL GAP IDENTIFICATION",
    ]
    for _ in range(3):
        doc.add_paragraph()
    for line in title_lines:
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(line)
        set_run_font(r, size=14, bold=True)
    for _ in range(3):
        doc.add_paragraph()
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Final Thesis Report"); set_run_font(r, size=13, bold=True)
    for _ in range(2):
        doc.add_paragraph()
    for line in ["Chapter 01 - Introduction", "Chapter 02 - Literature Review", "Chapter 03 - Methodology",
                 "Chapter 04 - System Requirement Specification", "Chapter 05 - Implementation / Designing",
                 "Chapter 06 - Testing and Evaluation", "Chapter 07 - Concluding Remarks"]:
        p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(line); set_run_font(r, size=11)
    for _ in range(3):
        doc.add_paragraph()
    for line in ["presented to the Faculty of Computing", "NSBM Green University"]:
        p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(line); set_run_font(r, size=12)
    doc.add_paragraph()
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("in partial fulfillment of the requirements for the degree of"); set_run_font(r, size=11)
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("BSc(Hons) in Software Engineering"); set_run_font(r, size=12, bold=True)
    for _ in range(3):
        doc.add_paragraph()
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("by"); set_run_font(r, size=11)
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Kalpani D. Kapuge"); set_run_font(r, size=13, bold=True)
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("Student ID: 28870"); set_run_font(r, size=12)
    for _ in range(3):
        doc.add_paragraph()
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("[Month] 2026"); set_run_font(r, size=11, italic=True)


def declaration_page(doc):
    add_chapter_title(doc, "Declaration", new_page=True)
    add_body_paragraph(doc,
        'I hereby declare that this thesis titled "Intelligent Recruitment Automation System for Job '
        'Description Creation, Application Shortlisting, and Skill Gap Identification" is based on my own '
        'research work carried out for the BSc(Hons) in Software Engineering degree program at NSBM Green '
        'University. The content presented in this document has been prepared for academic purposes, and all '
        "referenced work has been acknowledged through citations and the reference list. This report extends "
        "my previously submitted Interim Submission 02 report with the System Requirement Specification, "
        "Implementation, Testing and Evaluation, and Concluding Remarks chapters.", Tracker())
    doc.add_paragraph()
    for line in ["Student Name: Kalpani D. Kapuge", "Student ID: 28870", "Signature: ...........................",
                 "Date: ..........................."]:
        p = doc.add_paragraph()
        r = p.add_run(line)
        set_run_font(r, size=12)


KEYWORDS = sorted([
    "Artificial Intelligence", "Candidate Feedback", "Chatbot", "CV Generation",
    "Interview Scheduling", "Job Matching", "Recruitment Automation", "Skill Gap Analysis",
])


def abstract_page(doc):
    add_chapter_title(doc, "Abstract", new_page=True)
    t = Tracker()
    abstract_paras = [
        "Recruitment forms an integral part of the organizational process that has a direct impact on the "
        "quality of the workforce, organizational effectiveness, and overall business performance. Manual "
        "methods of job description writing, resume checking, candidate shortlisting, interview coordination, "
        "and subjective candidate assessment are still followed in conventional recruitment processes. These "
        "activities frequently result in delays, inconsistencies, bias, and a lack of transparency toward "
        "applicants. Candidates who are unsuccessful in an application rarely receive feedback on what they "
        "need to improve to enhance their employability.",
        "Recent advancements in Artificial Intelligence, Natural Language Processing, Machine Learning, and "
        "Large Language Models create new opportunities to strengthen recruitment automation: writing job "
        "descriptions, screening resumes, matching jobs to candidates by meaning rather than keyword, ranking "
        "candidates, identifying skill gaps, generating feedback, and assisting candidates through a chatbot. "
        "However, most existing recruitment tools treat these capabilities as standalone features rather than "
        "an integrated platform that supports candidates, employers, and administrators across the full "
        "recruitment lifecycle.",
        "This thesis presents the design, implementation, and evaluation of the Intelligent Recruitment "
        "Automation System (IRAS) — a web-based recruitment automation platform comprising candidate profile "
        "management, resume upload and AI-based parsing, an employer job-posting workflow with AI-assisted job "
        "description generation, resume shortlisting, a multi-signal candidate ranking and matching engine, "
        "skill gap analysis with AI-generated explanations, a CV builder, interview scheduling, a notification "
        "system, candidate feedback generation, a recruitment knowledge base, and a role-aware recruitment "
        "chatbot. The system is organized into three primary workflows — Candidate, Employer, and Admin — that "
        "together streamline recruitment activity for employers while giving applicants transparency and "
        "developmental guidance.",
        "The system follows a layered architecture (API, Application, Domain, and Infrastructure) built with "
        "ASP.NET Core Web API and C#, Entity Framework Core, a relational database, JWT-based authentication, "
        "Swagger/OpenAPI documentation, a companion Python AI microservice, configurable file storage, and PDF "
        "generation for CV export. The research follows Design Science Research Methodology, which is "
        "appropriate for the design, development, demonstration, and evaluation of a software artifact. This "
        "thesis reports the complete research and engineering cycle: the problem background and objectives "
        "(Chapter 1), a critical literature review (Chapter 2), the research methodology (Chapter 3), the "
        "system requirement specification (Chapter 4), implementation details including the trained "
        "candidate-job fit classifier (Chapter 5), functional, non-functional, and machine-learning model "
        "evaluation (Chapter 6), and concluding reflections with future recommendations (Chapter 7).",
        "The evaluation shows that combining structured features (skill overlap and semantic similarity) with "
        "free text substantially outperforms text-only classification (98.9% vs. 72.3% test accuracy for the "
        "deployed XGBoost model), that the system's functional and non-functional requirements are "
        "satisfied under the defined test plan, and that an integrated, AI-assisted recruitment platform can "
        "measurably reduce manual screening effort while giving candidates explainable, actionable feedback on "
        "their skill gaps.",
    ]
    for para in abstract_paras:
        add_body_paragraph(doc, para, t)
    doc.add_paragraph()
    p = doc.add_paragraph()
    r = p.add_run("Keywords: ")
    set_run_font(r, size=12, bold=True)
    r2 = p.add_run(", ".join(KEYWORDS))
    set_run_font(r2, size=12, italic=True)
    return t.word_count


ABBREVIATIONS = [
    ("AI", "Artificial Intelligence"), ("API", "Application Programming Interface"),
    ("ATS", "Applicant Tracking System"), ("CV", "Curriculum Vitae"),
    ("DSR", "Design Science Research"), ("DSRM", "Design Science Research Methodology"),
    ("EF Core", "Entity Framework Core"), ("F1-score", "Harmonic Mean of Precision and Recall"),
    ("FR", "Functional Requirement"), ("JWT", "JSON Web Token"),
    ("LLM", "Large Language Model"), ("ML", "Machine Learning"),
    ("NER", "Named Entity Recognition"), ("NFR", "Non-Functional Requirement"),
    ("NLP", "Natural Language Processing"), ("PDF", "Portable Document Format"),
    ("S-BERT", "Sentence-Bidirectional Encoder Representations from Transformers"),
    ("SQL", "Structured Query Language"), ("SRS", "System Requirement Specification"),
    ("SVM", "Support Vector Machine"), ("UI", "User Interface"),
    ("UML", "Unified Modeling Language"), ("UX", "User Experience"),
    ("XGBoost", "Extreme Gradient Boosting"),
]


def list_of_abbreviations(doc):
    add_chapter_title(doc, "List of Abbreviations", new_page=True)
    t = Tracker()
    rows = ["| Abbreviation | Description |"] + [f"| {a} | {d} |" for a, d in ABBREVIATIONS]
    add_table(doc, rows, t)


FIG_RE = re.compile(r"^FIG(?:WIDE|PEND)?:(.+)$", re.M)
TCAP_RE = re.compile(r"^TCAP:(.+)$", re.M)


def prescan_captions():
    figures, tables = [], []
    for fname in CHAPTER_FILES + ["appendices.txt"]:
        with open(os.path.join(CONTENT, fname), "r", encoding="utf-8") as f:
            text = f.read()
        for m in FIG_RE.finditer(text):
            _, label, caption = m.group(1).split("|", 2)
            figures.append((label.strip(), caption.strip()))
        for m in TCAP_RE.finditer(text):
            label, caption = m.group(1).split("|", 1)
            tables.append((label.strip(), caption.strip()))
    return figures, tables


def add_entry_list(doc, entries):
    """List of Figures / List of Tables — each entry is a real internal
    hyperlink (Ctrl+click, or a single click once Word's link-follow is on)
    to the bookmark planted on that figure/table's caption paragraph."""
    t = Tracker()
    for label, caption in entries:
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(6)
        p.paragraph_format.left_indent = Inches(0.2)
        p.paragraph_format.first_line_indent = Inches(-0.2)
        short = caption.split(". ")[0]
        if not short.endswith("."):
            short += "."
        add_hyperlink_run(p, bookmark_name(label), f"{label}: ", size=11, bold=True)
        add_hyperlink_run(p, bookmark_name(label), short, size=11, bold=False)
        t.word_count += len(caption.split())


# --------------------------------------------------------------------- main
def main():
    doc = Document()
    set_default_style(doc)
    setup_heading_styles(doc)
    add_footer_page_numbers(doc)

    title_page(doc)
    declaration_page(doc)
    abstract_word_count = abstract_page(doc)

    add_chapter_title(doc, "Table of Contents", new_page=True)
    add_toc_field(doc)

    figures_prescan, tables_prescan = prescan_captions()

    add_chapter_title(doc, "List of Figures", new_page=True)
    add_entry_list(doc, figures_prescan)

    add_chapter_title(doc, "List of Tables", new_page=True)
    add_entry_list(doc, tables_prescan)

    list_of_abbreviations(doc)

    tracker = Tracker()
    tracker.word_count += abstract_word_count

    for fname in CHAPTER_FILES:
        path = os.path.join(CONTENT, fname)
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()
        render_markup(doc, text, tracker, chapter_mode=True)

    add_chapter_title(doc, "References", new_page=True)
    ref_note = doc.add_paragraph()
    ref_note.paragraph_format.space_after = Pt(10)
    rn = ref_note.add_run("IEEE reference style. In-text citations appear as bracketed numbers, e.g. [4], keyed to the numbered list below.")
    set_run_font(rn, size=10, italic=True, color=RGBColor(90, 90, 90))
    with open(os.path.join(CONTENT, "references.txt"), "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            p = doc.add_paragraph()
            p.paragraph_format.space_after = Pt(8)
            p.paragraph_format.left_indent = Inches(0.35)
            p.paragraph_format.first_line_indent = Inches(-0.35)
            add_inline_runs(p, line, size=11)

    with open(os.path.join(CONTENT, "appendices.txt"), "r", encoding="utf-8") as f:
        appendix_text = f.read()
    render_markup(doc, appendix_text, tracker, chapter_mode=True)

    candidates = [OUT_PATH, OUT_PATH.replace(".docx", "_v2.docx"),
                  OUT_PATH.replace(".docx", "_v3.docx"), OUT_PATH.replace(".docx", "_final.docx")]
    saved_path = None
    for cand in candidates:
        try:
            doc.save(cand)
            saved_path = cand
            if cand != OUT_PATH:
                print(f"NOTE: earlier target(s) locked (likely open in Word) — saved to {saved_path} instead.")
            break
        except PermissionError:
            continue
    if saved_path is None:
        raise RuntimeError("All candidate output paths are locked — close the .docx files open in Word and re-run.")

    # ---- report back: figure/table inventory + word count ----
    print(f"Saved: {saved_path}")
    print(f"Body word count (approx, excludes front matter formatting labels): {tracker.word_count}")
    print(f"Figures inserted: {len(tracker.figures)}")
    for lab, cap in tracker.figures:
        print("  ", lab, "-", cap[:70])
    print(f"Tables inserted: {len(tracker.tables)}")
    for lab, cap in tracker.tables:
        print("  ", lab, "-", cap[:70])

    with open(os.path.join(ROOT, "toc_inventory.txt"), "w", encoding="utf-8") as f:
        f.write("FIGURES\n")
        for lab, cap in tracker.figures:
            f.write(f"{lab}: {cap}\n")
        f.write("\nTABLES\n")
        for lab, cap in tracker.tables:
            f.write(f"{lab}: {cap}\n")


if __name__ == "__main__":
    main()
