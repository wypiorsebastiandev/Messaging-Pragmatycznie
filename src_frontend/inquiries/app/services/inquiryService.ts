import { Inquiry } from '@/app/types/inquiry';

const API_URL = 'http://localhost:5011';

interface PaginatedResponse {
  data: Inquiry[];
  total: number;
}

export async function createInquiry(inquiryData: Partial<Inquiry>): Promise<void> {
  const response = await fetch(`${API_URL}/inquiries/submit`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(inquiryData),
  });

  if (!response.ok) {
    throw new Error('Failed to create inquiry');
  }

  return;
}

export const getPaginatedInquiries = async (pageIndex: number, pageSize: number): Promise<PaginatedResponse> => {
  const response = await fetch(
    `${API_URL}/inquiries?page=${pageIndex + 1}&limit=${pageSize}`
  );
  
  if (!response.ok) {
    throw new Error('Failed to fetch inquiries');
  }

  return await response.json();
}

export async function addInquiryComment(inquiryId: string, comment: string): Promise<void> {
  const response = await fetch(`${API_URL}/inquiries/${inquiryId}/comments`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ comment }),
  });

  if (!response.ok) {
    throw new Error('Failed to add comment');
  }

  return;
} 