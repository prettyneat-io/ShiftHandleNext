import type { Col } from '../components/forms/FormTable.vue';
import { useNotificationStore } from '../stores/notification';

type ExportFormat = 'csv' | 'pdf';

interface ExportConfig {
  endpoint: string;
  getColumns: () => Col[];
  fetchAllRows: (limitOverride?: number) => Promise<any[]>;
  getNestedValue: (row: any, path: string) => any;
  formatCell: (value: any, type?: Col['type']) => string;
}

function sanitizeFormattedValue(value: string) {
  return value === '—' ? '' : value;
}

function resolveColumns(allColumns: Col[], visibleKeys?: string[]) {
  if (!visibleKeys || visibleKeys.length === 0) {
    return allColumns.filter(col => col.key !== 'actions');
  }
  const selected = new Set(visibleKeys);
  return allColumns.filter(col => col.key !== 'actions' && selected.has(col.key));
}

function buildMatrix(columns: Col[], rows: any[], getNestedValue: ExportConfig['getNestedValue'], formatCell: ExportConfig['formatCell']) {
  return rows.map(row => columns.map(col => {
    const raw = getNestedValue(row, col.displayPath || col.key);
    const formatted = formatCell(raw, col.type);
    return sanitizeFormattedValue(formatted);
  }));
}

function buildFilename(endpoint: string, extension: ExportFormat) {
  const now = new Date();
  const pad = (value: number) => value.toString().padStart(2, '0');
  const timestamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}-${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  return `${endpoint}-${timestamp}.${extension}`;
}

function downloadBlob(blob: Blob, filename: string) {
  if (typeof window === 'undefined') return;
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

function escapeCsv(value: string) {
  const sanitized = value.replace(/\r?\n/g, ' ').replace(/"/g, '""');
  if (/[",]/.test(sanitized) || /^\s|\s$/.test(sanitized)) {
    return `"${sanitized}"`;
  }
  return sanitized;
}

function createCsv(columns: Col[], matrix: string[][]) {
  const header = columns.map(col => escapeCsv(col.label)).join(',');
  const rows = matrix.map(row => row.map(cell => escapeCsv(cell)).join(','));
  const contents = [header, ...rows].join('\r\n');
  return new Blob([contents], { type: 'text/csv;charset=utf-8;' });
}

function escapePdfText(value: string) {
  return value
    .replace(/\\/g, '\\\\')
    .replace(/\(/g, '\\(')
    .replace(/\)/g, '\\)')
    .replace(/\r?\n/g, ' ');
}

function createPdf(columns: Col[], matrix: string[][]) {
  const headerLine = columns.map(col => col.label).join(' | ');
  const lines: string[] = [headerLine];
  if (matrix.length === 0) {
    lines.push('No data available');
  } else {
    matrix.forEach(row => lines.push(row.join(' | ')));
  }

  const pageHeight = 792;
  const margin = 40;
  const lineHeight = 16;
  const linesPerPage = Math.max(1, Math.floor((pageHeight - margin * 2) / lineHeight));
  const pages: string[][] = [];
  for (let i = 0; i < lines.length; i += linesPerPage) {
    pages.push(lines.slice(i, i + linesPerPage));
  }
  if (pages.length === 0) {
    pages.push(['No data available']);
  }

  const pageCount = pages.length;
  const firstPageId = 3;
  const firstContentId = firstPageId + pageCount;
  const fontId = firstContentId + pageCount;
  const objects: string[] = [];

  objects.push('<< /Type /Catalog /Pages 2 0 R >>\n');
  const kids = pages.map((_, index) => `${firstPageId + index} 0 R`).join(' ');
  objects.push(`<< /Type /Pages /Kids [${kids}] /Count ${pageCount} >>\n`);

  pages.forEach((_, index) => {
    const contentId = firstContentId + index;
    objects.push(`<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents ${contentId} 0 R /Resources << /Font << /F1 ${fontId} 0 R >> >> >>\n`);
  });

  const encoder = new TextEncoder();
  pages.forEach(pageLines => {
    let y = pageHeight - margin;
    const contentLines = ['BT', '/F1 12 Tf'];
    pageLines.forEach(line => {
      const escaped = escapePdfText(line);
      contentLines.push(`1 0 0 1 ${margin} ${y.toFixed(2)} Tm (${escaped}) Tj`);
      y -= lineHeight;
    });
    contentLines.push('ET');
    const stream = contentLines.join('\n') + '\n';
    const length = encoder.encode(stream).length;
    objects.push(`<< /Length ${length} >>\nstream\n${stream}endstream\n`);
  });

  objects.push('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n');

  let pdf = '%PDF-1.4\n';
  const offsets: number[] = [0];
  objects.forEach((body, index) => {
    const id = index + 1;
    const object = `${id} 0 obj\n${body}endobj\n`;
    offsets.push(pdf.length);
    pdf += object;
  });

  const xrefOffset = pdf.length;
  const totalObjects = objects.length;
  pdf += `xref\n0 ${totalObjects + 1}\n`;
  pdf += '0000000000 65535 f \n';
  for (let i = 1; i <= totalObjects; i++) {
    pdf += `${String(offsets[i]).padStart(10, '0')} 00000 n \n`;
  }
  pdf += `trailer\n<< /Size ${totalObjects + 1} /Root 1 0 R >>\nstartxref\n${xrefOffset}\n%%EOF`;

  return new Blob([pdf], { type: 'application/pdf' });
}

async function uploadBlobToDrive(blob: Blob, filename: string, endpoint: string) {
  const mimeType = blob.type || (filename.endsWith('.csv') ? 'text/csv' : 'application/pdf');
  const formData = new FormData();
  formData.append('file', blob, filename);
  formData.append('mimeType', mimeType);
  formData.append('sourceEndpoint', endpoint);

  await $fetch('/api/exports/upload', {
    method: 'POST',
    body: formData,
  });
}

export function useExportData(config: ExportConfig) {
  const notificationStore = useNotificationStore();

  async function exportData(format: ExportFormat, visibleKeys?: string[]) {
    const columns = resolveColumns(config.getColumns(), visibleKeys);
    if (columns.length === 0) {
      throw new Error('No columns available to export.');
    }
    try {
      const rows = await config.fetchAllRows();
      const matrix = buildMatrix(columns, rows, config.getNestedValue, config.formatCell);
      const filename = buildFilename(config.endpoint, format);
      const blob = format === 'csv' ? createCsv(columns, matrix) : createPdf(columns, matrix);

      await uploadBlobToDrive(blob, filename, config.endpoint);
      notificationStore.addNotification({ message: 'Export uploaded to Drive successfully.', type: 'success' });

      downloadBlob(blob, filename);
    } catch (error: any) {
      const message = error?.data?.error || error?.message || 'Failed to upload export.';
      notificationStore.addNotification({ message, type: 'error' });
      throw error;
    }
  }

  return { exportData };
}
