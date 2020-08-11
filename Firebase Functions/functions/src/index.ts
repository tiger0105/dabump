import * as functions from 'firebase-functions';
import admin = require('firebase-admin');

admin.initializeApp();
const firestore = admin.firestore();

export const fetchCourts = functions.https.onRequest(async (request, response) => {
    const courtsReference = firestore.collection('Courts');
    const snapshot = await courtsReference.get();
    let data: FirebaseFirestore.DocumentData[] = [];
    snapshot.forEach(doc => {
        console.log(doc.id, '=>', doc.data());
        data.push(doc.data());
    });
    response.send("success");
});
