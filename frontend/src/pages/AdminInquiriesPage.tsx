import { useState, useEffect } from 'react';
import {
  Container,
  Typography,
  Paper,
  Box,
  Tabs,
  Tab,
  Divider,
  TextField,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import { inquiryService } from '../services/api';

// Define type for Inquiry
interface Inquiry {
  id: number;
  userId: number;
  vehicleId: number;
  subject: string;
  message: string;
  response?: string;
  dateSent: string;
  dateReplied?: string;
  status: 'New' | 'Read' | 'Replied' | 'Closed';
  user?: {
    id: number;
    name?: string;
    username?: string;
    firstName?: string;
    lastName?: string;
  };
  vehicle?: {
    id: number;
    make: string;
    model: string;
    year: number;
  };
}

interface ReplyData {
  response: string;
}

const AdminInquiriesPage = () => {
  const [loading, setLoading] = useState<boolean>(true);
  const [inquiries, setInquiries] = useState<Inquiry[]>([]);
  const [status, setStatus] = useState<string>('New'); // Default to showing New inquiries
  const [replyOpen, setReplyOpen] = useState<boolean>(false);
  const [currentInquiry, setCurrentInquiry] = useState<Inquiry | null>(null);
  const [replyText, setReplyText] = useState<string>('');
  const [submitting, setSubmitting] = useState<boolean>(false);

  useEffect(() => {
    const fetchInquiries = async () => {
      try {
        setLoading(true);
        const data = await inquiryService.getAllInquiries(status);
        setInquiries(data);
      } catch (error) {
        console.error('Error fetching inquiries:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchInquiries();
  }, [status]);

  const handleStatusChange = (
    _event: React.SyntheticEvent,
    newValue: string
  ) => {
    setStatus(newValue);
  };

  const handleReply = (inquiry: Inquiry) => {
    setCurrentInquiry(inquiry);
    setReplyOpen(true);
  };

  const handleMarkAsRead = async (id: number) => {
    try {
      await inquiryService.markInquiryAsRead(id);
      // Update the local state
      setInquiries(
        inquiries.map((inq) =>
          inq.id === id ? { ...inq, status: 'Read' as const } : inq
        )
      );
    } catch (error) {
      console.error('Error marking inquiry as read:', error);
    }
  };

  const submitReply = async () => {
    if (!replyText.trim() || !currentInquiry) return;

    try {
      setSubmitting(true);
      const replyData: ReplyData = {
        response: replyText,
      };
      await inquiryService.replyToInquiry(currentInquiry.id, replyData);

      // Update local state
      setInquiries(
        inquiries.map((inq) =>
          inq.id === currentInquiry.id
            ? {
                ...inq,
                status: 'Replied' as const,
                response: replyText,
                dateReplied: new Date().toISOString(),
              }
            : inq
        )
      );

      // Close dialog and reset
      setReplyOpen(false);
      setReplyText('');
      setCurrentInquiry(null);
    } catch (error) {
      console.error('Error sending reply:', error);
    } finally {
      setSubmitting(false);
    }
  };

  // Helper function to format user name
  const formatUserName = (user?: Inquiry['user']): string => {
    if (!user) return 'Unknown';
    if (user.name) return user.name;
    if (user.firstName && user.lastName)
      return `${user.firstName} ${user.lastName}`;
    return user.username || 'Unknown';
  };

  return (
    <Container maxWidth="lg">
      <Typography variant="h4" component="h1" gutterBottom>
        Manage Customer Inquiries
      </Typography>
      <Divider sx={{ mb: 3 }} />

      <Paper elevation={2} sx={{ p: 0, borderRadius: 2, overflow: 'hidden' }}>
        <Tabs
          value={status}
          onChange={handleStatusChange}
          variant="fullWidth"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="New" value="New" />
          <Tab label="Read" value="Read" />
          <Tab label="Replied" value="Replied" />
          <Tab label="Closed" value="Closed" />
        </Tabs>

        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress />
          </Box>
        ) : inquiries.length === 0 ? (
          <Box sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="body1" color="text.secondary">
              No {status.toLowerCase()} inquiries found
            </Typography>
          </Box>
        ) : (
          <Box>
            {inquiries.map((inquiry) => (
              <Paper
                key={inquiry.id}
                elevation={0}
                sx={{
                  p: 3,
                  borderBottom: '1px solid',
                  borderColor: 'divider',
                  '&:last-child': { borderBottom: 'none' },
                }}
              >
                <Box
                  sx={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    mb: 1,
                  }}
                >
                  <Typography variant="h6">{inquiry.subject}</Typography>
                  <Chip
                    label={inquiry.status}
                    color={
                      inquiry.status === 'New'
                        ? 'primary'
                        : inquiry.status === 'Read'
                          ? 'warning'
                          : inquiry.status === 'Replied'
                            ? 'success'
                            : 'default'
                    }
                    size="small"
                  />
                </Box>

                <Typography
                  variant="caption"
                  color="text.secondary"
                  display="block"
                  mb={2}
                >
                  From: {formatUserName(inquiry.user)} • Vehicle:{' '}
                  {inquiry.vehicle?.year} {inquiry.vehicle?.make}{' '}
                  {inquiry.vehicle?.model} • Sent:{' '}
                  {new Date(inquiry.dateSent).toLocaleString()}
                </Typography>

                <Typography variant="body1" sx={{ mb: 2 }}>
                  {inquiry.message}
                </Typography>

                {inquiry.response && (
                  <Paper
                    variant="outlined"
                    sx={{ p: 2, mb: 2, bgcolor: 'background.subtle' }}
                  >
                    <Typography variant="subtitle2" color="primary.main">
                      Your Reply (
                      {inquiry.dateReplied &&
                        new Date(inquiry.dateReplied).toLocaleString()}
                      ):
                    </Typography>
                    <Typography variant="body2">{inquiry.response}</Typography>
                  </Paper>
                )}

                <Box
                  sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}
                >
                  {inquiry.status === 'New' && (
                    <Button
                      variant="outlined"
                      color="warning"
                      size="small"
                      onClick={() => handleMarkAsRead(inquiry.id)}
                    >
                      Mark as Read
                    </Button>
                  )}

                  {(inquiry.status === 'New' || inquiry.status === 'Read') && (
                    <Button
                      variant="contained"
                      color="primary"
                      size="small"
                      onClick={() => handleReply(inquiry)}
                    >
                      Reply
                    </Button>
                  )}
                </Box>
              </Paper>
            ))}
          </Box>
        )}
      </Paper>

      {/* Reply Dialog */}
      <Dialog
        open={replyOpen}
        onClose={() => setReplyOpen(false)}
        fullWidth
        maxWidth="md"
      >
        <DialogTitle>Reply to Inquiry</DialogTitle>
        <DialogContent>
          {currentInquiry && (
            <>
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle1" fontWeight="bold">
                  Subject: {currentInquiry.subject}
                </Typography>
                <Typography variant="body2" paragraph>
                  From: {formatUserName(currentInquiry.user)} about{' '}
                  {currentInquiry.vehicle?.year} {currentInquiry.vehicle?.make}{' '}
                  {currentInquiry.vehicle?.model}
                </Typography>
                <Typography variant="body2" paragraph>
                  {currentInquiry.message}
                </Typography>
              </Box>

              <TextField
                fullWidth
                label="Your Reply"
                multiline
                rows={6}
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                variant="outlined"
                disabled={submitting}
              />
            </>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReplyOpen(false)} disabled={submitting}>
            Cancel
          </Button>
          <Button
            onClick={submitReply}
            variant="contained"
            color="primary"
            disabled={!replyText.trim() || submitting}
          >
            {submitting ? <CircularProgress size={24} /> : 'Send Reply'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default AdminInquiriesPage;
