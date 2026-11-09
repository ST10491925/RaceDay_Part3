// RaceDay stand-in API for running and demonstrating the Part 3 MVC site.
// Routes match RaceDay.Web/Services/ApiRoutes.cs. Data is saved to data.json.
// Uploaded images are saved to ./uploads (a local stand-in for Azure Blob Storage).

const express = require('express');
const jwt = require('jsonwebtoken');
const multer = require('multer');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const PORT = process.env.PORT || 5080;
const SECRET = process.env.JWT_SECRET || 'raceday-standin-dev-secret-key-change-me-0123456789';
const PUBLIC_URL = process.env.PUBLIC_URL || `http://localhost:${PORT}`;
const DATA_FILE = path.join(__dirname, 'data.json');
const UPLOAD_DIR = path.join(__dirname, 'uploads');
fs.mkdirSync(UPLOAD_DIR, { recursive: true });

// ---------- storage ----------
function hash(password, salt = crypto.randomBytes(16).toString('hex')) {
  const h = crypto.scryptSync(password, salt, 32).toString('hex');
  return `${salt}:${h}`;
}
function verify(password, stored) {
  const [salt, h] = stored.split(':');
  const test = crypto.scryptSync(password, salt, 32).toString('hex');
  return crypto.timingSafeEqual(Buffer.from(h, 'hex'), Buffer.from(test, 'hex'));
}
function iso(d) { return d.toISOString().slice(0, 19); } // no zone, like a local date time

function seed() {
  const day = 24 * 3600 * 1000, now = Date.now();
  const at = (days, h, m = 0) => { const d = new Date(now + days * day); d.setUTCHours(h, m, 0, 0); return iso(d); };
  const db = {
    seq: { user: 1, event: 1, category: 1, eventCategory: 1, enrolment: 1, result: 1 },
    users: [], events: [], categories: [], eventCategories: [], enrolments: [], results: []
  };
  const user = (firstName, lastName, email, password, role) => {
    const u = { id: db.seq.user++, firstName, lastName, email, phoneNumber: '', role, passwordHash: hash(password), profileImageUrl: null };
    db.users.push(u); return u;
  };
  const org = user('Race', 'Organiser', 'organiser@raceday.test', 'Organiser123!', 'Organiser');
  const p1 = user('Thandi', 'Zulu', 'thandi@raceday.test', 'Runner1234!', 'Participant');
  const p2 = user('Sipho', 'Ndlovu', 'sipho@raceday.test', 'Runner1234!', 'Participant');
  const p3 = user('Aisha', 'Khan', 'aisha@raceday.test', 'Runner1234!', 'Participant');

  const cat = (name, categoryType, o = {}) => {
    const c = { id: db.seq.category++, name, categoryType, description: '', minAge: null, maxAge: null, distanceKm: null, ...o };
    db.categories.push(c); return c;
  };
  const junior = cat('Junior', 'Age', { minAge: 12, maxAge: 17 });
  const senior = cat('Senior', 'Age', { minAge: 18, maxAge: 49 });
  const master = cat('Master', 'Age', { minAge: 50 });
  const fiveK = cat('5 km', 'Distance', { distanceKm: 5 });
  const tenK = cat('10 km', 'Distance', { distanceKm: 10 });

  const ev = (name, description, location, eventDate, distanceKm, eventType) => {
    const e = { id: db.seq.event++, name, description, location, eventDate, distanceKm, eventType, bannerImageUrl: null, organiserId: org.id };
    db.events.push(e); return e;
  };
  const dusi = ev('Durban Beachfront 10K', 'A flat, fast 10 km along the Golden Mile with sea views the whole way. Water points every 2.5 km and a finish on North Beach.', 'North Beach, Durban', at(21, 5, 30), 10, 'Run');
  const botanic = ev('Botanic Gardens Family Walk', 'A relaxed 5 km walk through the gardens. Prams, dogs on leads and first-timers all welcome.', 'Durban Botanic Gardens', at(35, 7), 5, 'Walk');
  const umhlanga = ev('Umhlanga Sunrise 5K', 'Catch the sunrise on the promenade. A quick 5 km for all ages.', 'Umhlanga Rocks', at(60, 5, 45), 5, 'Run');
  const past = ev('Berea Park Fun Run', 'Last month\'s fun run. Results are published.', 'Berea Park, Durban', at(-14, 6), 5, 'Run');

  const link = (e, c) => { const x = { id: db.seq.eventCategory++, eventId: e.id, categoryId: c.id }; db.eventCategories.push(x); return x; };
  [junior, senior, master, tenK].forEach(c => link(dusi, c));
  [senior, master, fiveK].forEach(c => link(botanic, c));
  [junior, senior, fiveK].forEach(c => link(umhlanga, c));
  const pj = link(past, junior), ps = link(past, senior), pm = link(past, master);

  const enrol = (u, e, ec, status = 'Confirmed') => {
    const x = { id: db.seq.enrolment++, userId: u.id, eventId: e.id, eventCategoryId: ec.id, status, enrolledAt: iso(new Date(now - 20 * day)) };
    db.enrolments.push(x); return x;
  };
  const e1 = enrol(p1, past, ps), e2 = enrol(p2, past, pm), e3 = enrol(p3, past, pj);
  const res = (en, finishTime, position) => db.results.push({ id: db.seq.result++, enrolmentId: en.id, finishTime, position });
  res(e1, '00:24:11', 1); res(e2, '00:27:45', 2); res(e3, '00:31:02', 3);
  enrol(p1, dusi, db.eventCategories.find(x => x.eventId === dusi.id && x.categoryId === senior.id));
  return db;
}

let db = fs.existsSync(DATA_FILE) ? JSON.parse(fs.readFileSync(DATA_FILE, 'utf8')) : seed();
const save = () => fs.writeFileSync(DATA_FILE, JSON.stringify(db, null, 2));
save();

// ---------- dto helpers ----------
const catDto = (c, ec) => ({
  eventCategoryId: ec ? ec.id : undefined, categoryId: c.id, id: ec ? ec.id : c.id,
  name: c.name, description: c.description, categoryType: c.categoryType,
  minAge: c.minAge, maxAge: c.maxAge, distanceKm: c.distanceKm
});
const eventCats = id => db.eventCategories.filter(x => x.eventId === id)
  .map(ec => catDto(db.categories.find(c => c.id === ec.categoryId), ec));
const eventDto = e => ({ ...e, categories: eventCats(e.id) });
const userName = u => u ? `${u.firstName} ${u.lastName}`.trim() : '';
const enrolDto = en => {
  const ev = db.events.find(e => e.id === en.eventId);
  const ec = db.eventCategories.find(x => x.id === en.eventCategoryId);
  const c = ec && db.categories.find(x => x.id === ec.categoryId);
  return {
    id: en.id, eventId: en.eventId, eventName: ev?.name, eventDate: ev?.eventDate,
    eventCategoryId: en.eventCategoryId, categoryName: c?.name, status: en.status,
    enrolledAt: en.enrolledAt, participantName: userName(db.users.find(u => u.id === en.userId))
  };
};
const resultDto = r => {
  const en = db.enrolments.find(x => x.id === r.enrolmentId);
  const d = enrolDto(en);
  const total = db.results.filter(x => db.enrolments.find(e => e.id === x.enrolmentId)?.eventId === en.eventId).length;
  return { id: r.id, enrolmentId: r.enrolmentId, eventId: d.eventId, eventName: d.eventName, eventDate: d.eventDate,
    categoryName: d.categoryName, participantName: d.participantName, finishTime: r.finishTime,
    position: r.position, totalFinishers: total };
};
const profileDto = u => ({ id: u.id, firstName: u.firstName, lastName: u.lastName, email: u.email,
  phoneNumber: u.phoneNumber, role: u.role, profileImageUrl: u.profileImageUrl });

// ---------- app ----------
const app = express();
app.use(express.json());
app.use('/uploads', express.static(UPLOAD_DIR));
app.use((req, res, next) => { res.set('Access-Control-Allow-Origin', '*'); next(); });

const bad = (res, message, status = 400) => res.status(status).json({ message });

const auth = (role) => (req, res, next) => {
  const h = req.headers.authorization || '';
  if (!h.startsWith('Bearer ')) return bad(res, 'Sign in required.', 401);
  try {
    const p = jwt.verify(h.slice(7), SECRET);
    req.user = db.users.find(u => u.id === Number(p.nameid));
    if (!req.user) return bad(res, 'Sign in required.', 401);
  } catch { return bad(res, 'Your session has expired.', 401); }
  if (role && req.user.role !== role) return bad(res, 'You do not have permission to do that.', 403);
  next();
};

const upload = multer({
  storage: multer.diskStorage({
    destination: UPLOAD_DIR,
    filename: (req, file, cb) => cb(null, crypto.randomBytes(12).toString('hex') + path.extname(file.originalname).toLowerCase())
  }),
  limits: { fileSize: 5 * 1024 * 1024 },
  fileFilter: (req, file, cb) => cb(null, ['image/jpeg', 'image/png', 'image/webp'].includes(file.mimetype))
});
const imageUpload = (req, res, next) => upload.single('file')(req, res, err => {
  if (err) return bad(res, err.code === 'LIMIT_FILE_SIZE' ? 'The image is larger than 5 MB.' : 'Upload failed.');
  if (!req.file) return bad(res, 'Upload a JPG, PNG or WebP image.');
  next();
});
const fileUrl = f => `${PUBLIC_URL}/uploads/${f.filename}`;

// auth
app.post('/api/auth/register', (req, res) => {
  const { firstName, lastName, email, phoneNumber, password } = req.body || {};
  if (!firstName || !lastName) return bad(res, 'First name and last name are required.');
  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email || '')) return bad(res, 'Enter a valid email address.');
  if (!password || password.length < 8) return bad(res, 'Password must be at least 8 characters.');
  if (db.users.some(u => u.email.toLowerCase() === email.toLowerCase())) return bad(res, 'An account with that email already exists.', 409);
  const u = { id: db.seq.user++, firstName: firstName.trim(), lastName: lastName.trim(), email: email.trim(),
    phoneNumber: phoneNumber || '', role: 'Participant', passwordHash: hash(password), profileImageUrl: null };
  db.users.push(u); save();
  res.status(201).json({ message: 'Registered.' });
});

app.post('/api/auth/login', (req, res) => {
  const { email, password } = req.body || {};
  const u = db.users.find(x => x.email.toLowerCase() === String(email || '').toLowerCase());
  if (!u || !verify(String(password || ''), u.passwordHash)) return bad(res, 'Invalid email or password.', 401);
  const token = jwt.sign({ nameid: String(u.id), unique_name: userName(u), email: u.email, role: u.role }, SECRET, { expiresIn: '8h' });
  res.json({ token, role: u.role, name: userName(u) });
});

// profile
app.get('/api/users/me', auth(), (req, res) => res.json(profileDto(req.user)));
app.put('/api/users/me', auth(), (req, res) => {
  const { firstName, lastName, phoneNumber } = req.body || {};
  if (!firstName || !lastName) return bad(res, 'First name and last name are required.');
  Object.assign(req.user, { firstName: firstName.trim(), lastName: lastName.trim(), phoneNumber: phoneNumber || '' });
  save(); res.json(profileDto(req.user));
});
app.post('/api/users/me/picture', auth(), imageUpload, (req, res) => {
  req.user.profileImageUrl = fileUrl(req.file); save();
  res.json({ profileImageUrl: req.user.profileImageUrl });
});

// events
function readEvent(body) {
  const { name, description, location, eventDate, distanceKm, eventType } = body || {};
  if (!name || !description || !location) return { error: 'Name, description and location are required.' };
  const d = new Date(eventDate);
  if (!eventDate || isNaN(d)) return { error: 'A valid event date is required.' };
  const km = Number(distanceKm);
  if (!(km > 0 && km <= 500)) return { error: 'Distance must be between 0.1 and 500 km.' };
  return { value: { name: name.trim(), description: description.trim(), location: location.trim(),
    eventDate: String(eventDate).slice(0, 19), distanceKm: km, eventType: eventType || 'Run' } };
}
const ownEvent = (req, res) => {
  const e = db.events.find(x => x.id === Number(req.params.id));
  if (!e) { bad(res, 'Event not found.', 404); return null; }
  if (e.organiserId !== req.user.id) { bad(res, 'You can only manage events that you created.', 403); return null; }
  return e;
};

app.get('/api/events', (req, res) => res.json(db.events.map(eventDto)));
app.get('/api/events/:id', (req, res) => {
  const e = db.events.find(x => x.id === Number(req.params.id));
  return e ? res.json(eventDto(e)) : bad(res, 'Event not found.', 404);
});
app.post('/api/events', auth('Organiser'), (req, res) => {
  const r = readEvent(req.body); if (r.error) return bad(res, r.error);
  const e = { id: db.seq.event++, ...r.value, bannerImageUrl: null, organiserId: req.user.id };
  db.events.push(e); save(); res.status(201).json(eventDto(e));
});
app.put('/api/events/:id', auth('Organiser'), (req, res) => {
  const e = ownEvent(req, res); if (!e) return;
  const r = readEvent(req.body); if (r.error) return bad(res, r.error);
  Object.assign(e, r.value); save(); res.json(eventDto(e));
});
app.delete('/api/events/:id', auth('Organiser'), (req, res) => {
  const e = ownEvent(req, res); if (!e) return;
  const ens = db.enrolments.filter(x => x.eventId === e.id).map(x => x.id);
  db.results = db.results.filter(r => !ens.includes(r.enrolmentId));
  db.enrolments = db.enrolments.filter(x => x.eventId !== e.id);
  db.eventCategories = db.eventCategories.filter(x => x.eventId !== e.id);
  db.events = db.events.filter(x => x.id !== e.id);
  save(); res.status(204).end();
});
app.post('/api/events/:id/banner', auth('Organiser'), (req, res, next) => {
  if (!ownEvent(req, res)) return; next();
}, imageUpload, (req, res) => {
  const e = db.events.find(x => x.id === Number(req.params.id));
  e.bannerImageUrl = fileUrl(req.file); save();
  res.json({ bannerImageUrl: e.bannerImageUrl });
});

// categories
app.get('/api/categories', (req, res) => res.json(db.categories.map(c => catDto(c))));
app.post('/api/categories', auth('Organiser'), (req, res) => {
  const { name, description, categoryType, minAge, maxAge, distanceKm } = req.body || {};
  if (!name) return bad(res, 'Category name is required.');
  const c = { id: db.seq.category++, name: name.trim(), description: description || '', categoryType: categoryType || 'Age',
    minAge: minAge ?? null, maxAge: maxAge ?? null, distanceKm: distanceKm ?? null };
  db.categories.push(c); save(); res.status(201).json(catDto(c));
});
app.get('/api/events/:id/categories', (req, res) => {
  const e = db.events.find(x => x.id === Number(req.params.id));
  return e ? res.json(eventCats(e.id)) : bad(res, 'Event not found.', 404);
});
app.post('/api/events/:id/categories', auth('Organiser'), (req, res) => {
  const e = ownEvent(req, res); if (!e) return;
  const c = db.categories.find(x => x.id === Number(req.body?.categoryId));
  if (!c) return bad(res, 'Category not found.', 404);
  if (db.eventCategories.some(x => x.eventId === e.id && x.categoryId === c.id)) return bad(res, 'That category is already on this event.', 409);
  const ec = { id: db.seq.eventCategory++, eventId: e.id, categoryId: c.id };
  db.eventCategories.push(ec); save(); res.status(201).json(catDto(c, ec));
});

// enrolments
app.post('/api/enrolments', auth('Participant'), (req, res) => {
  const eventId = Number(req.body?.eventId), ecId = Number(req.body?.eventCategoryId);
  const e = db.events.find(x => x.id === eventId);
  if (!e) return bad(res, 'Event not found.', 404);
  if (new Date(e.eventDate) < new Date()) return bad(res, 'This event has already taken place.');
  const ec = db.eventCategories.find(x => x.id === ecId && x.eventId === eventId);
  if (!ec) return bad(res, 'Choose a category that belongs to this event.');
  if (db.enrolments.some(x => x.userId === req.user.id && x.eventId === eventId && x.status !== 'Cancelled'))
    return bad(res, 'You are already entered in this event.', 409);
  const en = { id: db.seq.enrolment++, userId: req.user.id, eventId, eventCategoryId: ecId, status: 'Confirmed', enrolledAt: iso(new Date()) };
  db.enrolments.push(en); save(); res.status(201).json(enrolDto(en));
});
app.get('/api/enrolments/my', auth('Participant'), (req, res) =>
  res.json(db.enrolments.filter(x => x.userId === req.user.id).map(enrolDto)));
app.delete('/api/enrolments/:id', auth('Participant'), (req, res) => {
  const en = db.enrolments.find(x => x.id === Number(req.params.id) && x.userId === req.user.id);
  if (!en) return bad(res, 'Enrolment not found.', 404);
  en.status = 'Cancelled'; save(); res.status(204).end();
});
app.get('/api/events/:id/enrolments', auth('Organiser'), (req, res) => {
  const e = ownEvent(req, res); if (!e) return;
  res.json(db.enrolments.filter(x => x.eventId === e.id).map(enrolDto));
});

// results
const resultBody = (req, res) => {
  const { enrolmentId, finishTime, position } = req.body || {};
  if (!/^\d{1,2}:[0-5]\d:[0-5]\d$/.test(finishTime || '')) { bad(res, 'Finish time must be hh:mm:ss.'); return null; }
  if (!(Number(position) >= 1)) { bad(res, 'Position must be 1 or higher.'); return null; }
  return { enrolmentId: Number(enrolmentId), finishTime, position: Number(position) };
};
const enrolmentForOrganiser = (req, res, enrolmentId) => {
  const en = db.enrolments.find(x => x.id === enrolmentId);
  const e = en && db.events.find(x => x.id === en.eventId);
  if (!en) { bad(res, 'Enrolment not found.', 404); return null; }
  if (e.organiserId !== req.user.id) { bad(res, 'You can only record results for your own events.', 403); return null; }
  if (new Date(e.eventDate) > new Date()) { bad(res, 'Results can only be recorded after the event has concluded.'); return null; }
  return en;
};
app.post('/api/results', auth('Organiser'), (req, res) => {
  const b = resultBody(req, res); if (!b) return;
  const en = enrolmentForOrganiser(req, res, b.enrolmentId); if (!en) return;
  if (db.results.some(r => r.enrolmentId === en.id)) return bad(res, 'A result already exists for this enrolment.', 409);
  const r = { id: db.seq.result++, ...b }; db.results.push(r); save(); res.status(201).json(resultDto(r));
});
app.put('/api/results/:id', auth('Organiser'), (req, res) => {
  const r = db.results.find(x => x.id === Number(req.params.id));
  if (!r) return bad(res, 'Result not found.', 404);
  const b = resultBody(req, res); if (!b) return;
  if (!enrolmentForOrganiser(req, res, r.enrolmentId)) return;
  r.finishTime = b.finishTime; r.position = b.position; save(); res.json(resultDto(r));
});
app.get('/api/events/:id/results', auth('Organiser'), (req, res) => {
  const e = ownEvent(req, res); if (!e) return;
  const ens = db.enrolments.filter(x => x.eventId === e.id).map(x => x.id);
  res.json(db.results.filter(r => ens.includes(r.enrolmentId)).map(resultDto));
});
app.get('/api/results/my', auth('Participant'), (req, res) => {
  const mine = db.enrolments.filter(x => x.userId === req.user.id).map(x => x.id);
  res.json(db.results.filter(r => mine.includes(r.enrolmentId)).map(resultDto));
});

app.get('/', (req, res) => res.json({ name: 'RaceDay stand-in API', status: 'running' }));
app.use((req, res) => bad(res, 'Route not found.', 404));

app.listen(PORT, '0.0.0.0', () => {
  console.log(`RaceDay stand-in API listening on http://localhost:${PORT}`);
  console.log('Organiser: organiser@raceday.test / Organiser123!');
  console.log('Participant: thandi@raceday.test / Runner1234!');
});
