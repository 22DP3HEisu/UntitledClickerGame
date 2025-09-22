var express = require('express');
var router = express.Router();

/* GET users listing. */
router.get('/', function(req, res, next) {

});

router.post('/', function(req, res, next) {
  res.send('Create a new user');
});

module.exports = router;
