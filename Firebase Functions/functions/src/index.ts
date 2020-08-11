import * as functions from 'firebase-functions';
import admin = require('firebase-admin');

admin.initializeApp();

const firestore = admin.firestore();

export const fetchCourts = functions.https.onRequest(async (request, response) => {
    const courtsReference = firestore.collection('Courts');
    const snapshot = await courtsReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(async doc => {
        const docData = doc.data();
        data.push({ 'court': docData });
    });
    response.send(data);
});

export const fetchProfiles = functions.https.onRequest(async (request, response) => {
    const profilesReference = firestore.collection('Profiles');
    const snapshot = await profilesReference.get();
    const data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(async doc => {
        const docData = doc.data();
        data.push({ 'profile': docData });
    });
    response.send(data);
});