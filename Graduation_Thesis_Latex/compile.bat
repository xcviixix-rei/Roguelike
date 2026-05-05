@echo off
echo ==========================================
echo Compiling LaTeX Document...
echo ==========================================

REM First pass
echo [1/4] Running pdflatex (first pass)...
pdflatex -interaction=nonstopmode main.tex
if %errorlevel% neq 0 (
    echo ERROR: First pdflatex pass failed!
    pause
    exit /b %errorlevel%
)

REM BibTeX
echo [2/4] Running bibtex...
bibtex main
if %errorlevel% neq 0 (
    echo WARNING: BibTeX had issues, continuing...
)

REM Second pass
echo [3/4] Running pdflatex (second pass)...
pdflatex -interaction=nonstopmode main.tex
if %errorlevel% neq 0 (
    echo ERROR: Second pdflatex pass failed!
    pause
    exit /b %errorlevel%
)

REM Third pass (for references)
echo [4/4] Running pdflatex (third pass)...
pdflatex -interaction=nonstopmode main.tex
if %errorlevel% neq 0 (
    echo ERROR: Third pdflatex pass failed!
    pause
    exit /b %errorlevel%
)

echo ==========================================
echo SUCCESS! PDF generated: main.pdf
echo ==========================================

REM Open the PDF
start main.pdf

pause
