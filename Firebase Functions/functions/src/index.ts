import * as functions from 'firebase-functions';
import admin = require('firebase-admin');

admin.initializeApp();

const firestore = admin.firestore();
const storage = admin.storage();

export const fetchCourts = functions.https.onRequest(async (request, response) => {
    const courtsReference = firestore.collection('Courts');
    const snapshot = await courtsReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(async doc => {
        const docData = doc.data();
        const document = storage.bucket('dabump-8c59c.appspot.com').file(docData.Image);
        const [url] = await document.getSignedUrl({
            action: 'read',
            expires: '08-13-2020',
        });
        docData.Image = url;
        data.push({ 'court': docData });
    });
    response.send(data);
});

export const fetchProfiles = functions.https.onRequest(async (request, response) => {
    const profilesReference = firestore.collection('Profiles');
    const snapshot = await profilesReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(doc => {
        data.push({ 'profile': doc.data() });
    });
    response.send(Object.assign({}, data));
});